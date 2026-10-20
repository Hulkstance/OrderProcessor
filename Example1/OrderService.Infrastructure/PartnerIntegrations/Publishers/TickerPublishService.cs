using CryptoClients.Net.Enums;
using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Errors;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Symbols;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using System.Diagnostics;

namespace OrderService.Infrastructure.PartnerIntegrations.Publishers;

internal sealed class TickerPublishService : BackgroundService
{
    private CancellationToken _stoppingToken;
    private readonly ILogger _logger;
    private readonly IExchangeRestClient _restClient;
    private readonly IExchangeSocketClient _socketClient;
    private readonly IPublishOutput<TickerData> _publishOutput;
    private readonly double _pollInterval;
    private readonly DataBatcher<TickerData> _tickerBatcher;
    private readonly Dictionary<string, SharedSpotSymbol[]> _symbols = new();
    private readonly AsyncResetEvent _symbolsInitialSetEvent = new(false, false);

    private readonly string[]? _enabledExchanges;

    public TickerPublishService(
        ILogger<TickerPublishService> logger,
        IConfiguration configuration,
        IExchangeRestClient restClient,
        IExchangeSocketClient socketClient,
        IPublishOutput<TickerData> publishOutput)
    {
        _logger = logger;
        _restClient = restClient;
        _socketClient = socketClient;
        _publishOutput = publishOutput;

        _pollInterval = configuration.GetValue<double?>("TickersPollInterval") ?? 0.16;
        _enabledExchanges = configuration.GetSection("EnabledExchanges").Get<string[]>();

        _tickerBatcher = new DataBatcher<TickerData>(TimeSpan.FromSeconds(5), PublishTickers);
    }

    private async Task PublishTickers(Dictionary<string, TickerData> dictionary)
    {
        var exchangeData = new PublishItem<TickerData>();
        exchangeData.Data = dictionary.Values;
        await _publishOutput.PublishAsync(exchangeData);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        _logger.LogDebug("Starting TickerPublishService");

        await _tickerBatcher.StartAsync();

        // Init Symbol Cache
        _ = PollSymbolsAsync();
        await _symbolsInitialSetEvent.WaitAsync();

        // Subscribe websocket which exchange support this
        var subscribedExchanges = await SubscribeTickersAsync(_stoppingToken);
        var pollingExchanges = Exchange.All.Except(subscribedExchanges).ToList();
        if (_enabledExchanges?.Any() == true)
            pollingExchanges = pollingExchanges.Where(x => _enabledExchanges.Contains(x)).ToList();

        _logger.LogInformation("TickerPublishService {SubCount} exchange subscribed, starting polling for {PollCount}", subscribedExchanges.Count, pollingExchanges.Count);

        // For remaining exchanges use polling
        while (!_stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                await PollAsync(pollingExchanges);

                var waitTime = TimeSpan.FromMinutes(_pollInterval);
                waitTime = waitTime.Add(-sw.Elapsed);
                if (waitTime < TimeSpan.FromMilliseconds(1))
                    waitTime = TimeSpan.FromMilliseconds(1);

                try { await Task.Delay(waitTime, _stoppingToken); } catch { }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TickerPublishService polling loop exception");
            }
        }

        await _tickerBatcher.StopAsync();
        _logger.LogDebug("TickerPublishService stopped");
    }

    private async Task PollSymbolsAsync()
    {
        // Poll symbols on interval so we know which ones are no longer supported
        while (!_stoppingToken.IsCancellationRequested)
        {
            var results = await _restClient.GetSpotSymbolsAsync(new GetSymbolsRequest(), _enabledExchanges);
            foreach (var result in results.Where(x => x.Success))
                _symbols[result.Exchange] = result.Data;

            _symbolsInitialSetEvent.Set();
            try { await Task.Delay(TimeSpan.FromMinutes(15), _stoppingToken); } catch { }
        }
    }

    private async Task<List<string>> SubscribeTickersAsync(CancellationToken ct)
    {
        var exchanges = _enabledExchanges ?? Exchange.All;
        var subbedExchanges = new List<string>();
        var allTickerClients = _socketClient.GetTickersClients(TradingMode.Spot).Where(x => exchanges.Contains(x.Exchange));

        foreach (var tickerClient in allTickerClients)
        {
            _logger.LogDebug("TickerPublishService starting all ticker for {Exchange}", tickerClient.Exchange);
            var subResult = await tickerClient.SubscribeToAllTickersUpdatesAsync(new SubscribeAllTickersRequest(), ProcessUpdate, _stoppingToken);
            if (subResult)
            {
                AttachEventHandler(subResult.Data, $"{tickerClient.Exchange}.AllTickers");
                subbedExchanges.Add(subResult.Exchange);
            }
        }

        var multiTickerClients = _socketClient.GetTickerClients(TradingMode.Spot)
            .Where(x => !subbedExchanges.Contains(x.Exchange) && exchanges.Contains(x.Exchange) && x.SubscribeTickerOptions.SupportsMultipleSymbols);
        foreach (var tickerClient in multiTickerClients)
        {
            if (tickerClient.Exchange == "Kraken")
                // Kraken symbol mapping doesn't work great between REST and WebSocket, use polling instead
                continue;

            if (!_symbols!.TryGetValue(tickerClient.Exchange, out var exchangeSymbols))
                continue;

            var offset = 0;
            var perPage = tickerClient.SubscribeTickerOptions.MaxSymbolCount ?? exchangeSymbols.Length;

            var pages = Math.Ceiling(exchangeSymbols.Length / (double)perPage);
            if (pages > 10)
                // Needs more than 10 subs to subscribe to all symbols, just go with polling
                continue;

            var exchangeSubs = new List<UpdateSubscription>();
            _logger.LogDebug("TickerPublishService starting batched ticker for {Exchange} in {Pages} batches", tickerClient.Exchange, pages);
            var success = true;
            for (var i = 0; i < pages; i++)
            {
                var symbols = exchangeSymbols[offset..(offset + perPage)];
                var subResult = await SubscribeToTickersAsync(tickerClient, symbols, i);
                if (!subResult)
                {
                    await Task.WhenAll(exchangeSubs.Select(x => x.CloseAsync()));
                    success = false;
                    break;
                }

                exchangeSubs.Add(subResult.Data);
            }

            if (success)
                subbedExchanges.Add(tickerClient.Exchange);
        }

        return subbedExchanges;
    }

    private void AttachEventHandler(UpdateSubscription subscription, string topic)
    {
        subscription.SubscriptionStatusChanged += newStatus =>
        {
            if (newStatus == SubscriptionStatus.Subscribed)
                _logger.LogInformation("Subscription {Topic} restored to subscribed state", topic);
            else if (newStatus == SubscriptionStatus.Pending)
                _logger.LogWarning("Subscription {Topic} interrupted", topic);
        };
        subscription.Exception += ex => _logger.LogError(ex, "Subscription {Topic} exception", topic);
    }

    private async Task<ExchangeResult<UpdateSubscription>> SubscribeToTickersAsync(ITickerSocketClient tickerClient, SharedSpotSymbol[] symbols, int setNumber)
    {
        var subResult = await tickerClient.SubscribeToTickerUpdatesAsync(new SubscribeTickerRequest(symbols.Select(x => x.SharedSymbol)), ProcessUpdate, _stoppingToken);
        if (!subResult)
        {
            _logger.LogDebug("TickerPublishService batch ticker for {Exchange} failed", tickerClient.Exchange);
            return subResult;
        }

        AttachEventHandler(subResult.Data, $"{tickerClient.Exchange}.TickerSet.{setNumber}");
        subResult.Data.ResubscribingFailed += async (x) =>
        {
            if (x.ErrorType == ErrorType.UnknownSymbol
            || x.ErrorType == ErrorType.InvalidParameter)
            {
                _logger.LogError("TickerPublishService resubscribing failed with error {ErrorType}, checking valid symbols and resubscribing", x.ErrorType);
                await subResult.Data.CloseAsync();
                var validSymbols = GetValidSymbols(tickerClient.Exchange, symbols);

                var result = await SubscribeToTickersAsync(tickerClient, validSymbols, setNumber);
                var dropped = string.Join(", ", symbols.Select(s => s.Name).Except(validSymbols.Select(s => s.Name)));
                if (!result)
                    _logger.LogError("TickerPublishService resubscribing symbols failed; dropped symbols: [{Dropped}]", dropped);
                else
                    _logger.LogInformation("TickerPublishService resubscribing symbols succeeded; dropped symbols: [{Dropped}]", dropped);

                return;
            }

            _logger.LogError("TickerPublishService resubscribing failed with unexpected error {Error}", x.ToString());
        };

        return subResult;
    }

    private SharedSpotSymbol[] GetValidSymbols(string exchange, SharedSpotSymbol[] symbols)
    {
        var exchangeSymbols = _symbols[exchange];
        return symbols.Where(x => exchangeSymbols.Any(y => x.Name == y.Name && x.Trading)).ToArray();
    }

    private void ProcessUpdate(DataEvent<SharedSpotTicker> @event)
    {
        if (@event.Data.SharedSymbol == null)
            return;

        var tickerOptions = _restClient.GetSpotTickerClient(@event.Exchange)!.GetSpotTickersOptions;
        var data = new Dictionary<string, TickerData>
        {
            [@event.Exchange + @event.Data.Symbol] = ParseTicker(@event.Exchange, @event.Data, tickerOptions.TickerType)
        };
        _ = _tickerBatcher.AddAsync(data);
    }

    private void ProcessUpdate(DataEvent<SharedSpotTicker[]> @event)
    {
        var tickerOptions = _restClient.GetSpotTickerClient(@event.Exchange)!.GetSpotTickersOptions;
        var data = new Dictionary<string, TickerData>();
        foreach (var symbol in @event.Data)
        {
            if (symbol.SharedSymbol == null)
                continue;

            data.Add(@event.Exchange + symbol.Symbol, ParseTicker(@event.Exchange, symbol, tickerOptions.TickerType));
        }

        _ = _tickerBatcher.AddAsync(data);
    }

    private async Task PollAsync(List<string> exchanges)
    {
        var tickersTasks = _restClient.GetSpotTickersAsyncEnumerable(new GetTickersRequest(), exchanges, _stoppingToken);
        await foreach (var result in tickersTasks)
        {
            if (!result)
            {
                _logger.LogError("Failed to request tickers from exchange {Exchange}: {Error}", result.Exchange, result.Error!.ToString());
                continue;
            }

            var tickerOptions = _restClient.GetSpotTickerClient(result.Exchange)!.GetSpotTickersOptions;
            var data = new Dictionary<string, TickerData>();
            foreach (var symbol in result.Data)
            {
                if (symbol.SharedSymbol == null)
                    continue;

                data.Add(result.Exchange + symbol.Symbol, ParseTicker(result.Exchange, symbol, tickerOptions.TickerType));
            }
            await _tickerBatcher.AddAsync(data);
        }
    }

    private TickerData ParseTicker(string exchange, SharedSpotTicker ticker, SharedTickerType tickerType)
    {
        return new TickerData(
            Exchange:          exchange,
            BaseAsset:         ticker.SharedSymbol!.BaseAsset,
            QuoteAsset:        ticker.SharedSymbol.QuoteAsset,
            LastPrice:         ticker.LastPrice,
            HighPrice:         ticker.HighPrice,
            LowPrice:          ticker.LowPrice,
            Volume:            ticker.Volume,
            QuoteVolume:       ticker.QuoteVolume,
            ChangePercentage:  ticker.ChangePercentage,
            TickerType:        MapTickerType(tickerType));
    }

    private static TickerType MapTickerType(SharedTickerType tickerType) => tickerType switch
    {
        SharedTickerType.Day24H  => TickerType.Day24H,
        SharedTickerType.DayUtc0 => TickerType.DayUtc0,
        _                        => TickerType.Other
    };
}
