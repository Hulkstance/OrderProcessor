using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.SharedApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using System.Diagnostics;

namespace OrderService.Infrastructure.PartnerIntegrations.Publishers;

internal sealed class SymbolPublishService : BackgroundService
{
    private CancellationToken _stoppingToken = default;
    private readonly ILogger _logger;
    private readonly IExchangeRestClient _restClient;
    private readonly IPublishOutput<SymbolData> _publishOutput;
    private readonly int _pollInterval;
    private readonly string[]? _enabledExchanges;

    public SymbolPublishService(
        ILogger<SymbolPublishService> logger,
        IConfiguration configuration,
        IExchangeRestClient restClient,
        IPublishOutput<SymbolData> publishOutput)
    {
        _logger = logger;
        _restClient = restClient;
        _publishOutput = publishOutput;

        _pollInterval = configuration.GetValue<int?>("SymbolsPollInterval") ?? 15;
        _enabledExchanges = configuration.GetSection("EnabledExchanges").Get<string[]>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        _logger.LogDebug("Starting SymbolPublishService");
        while (!_stoppingToken.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();
            await ProcessAsync();

            var waitTime = TimeSpan.FromMinutes(_pollInterval);
            waitTime = waitTime.Add(-sw.Elapsed);
            if (waitTime < TimeSpan.FromMilliseconds(1))
                waitTime = TimeSpan.FromMilliseconds(1);

            try { await Task.Delay(waitTime, _stoppingToken); } catch { }
        }

        _logger.LogDebug("SymbolPublishService stopped");
    }

    private async Task ProcessAsync()
    {
        var symbolsTasks = _restClient.GetSpotSymbolsAsync(new GetSymbolsRequest(), _enabledExchanges, _stoppingToken);
        await Task.WhenAll(symbolsTasks);

        foreach (var result in symbolsTasks.Result)
        {
            if (!result)
            {
                _logger.LogError("Failed to request symbols from exchange {Exchange}: {Error}", result.Exchange, result.Error!.ToString());
                continue;
            }

            var exchangeData = new PublishItem<SymbolData>(result.Exchange);
            var data = new List<SymbolData>();
            foreach (var symbol in result.Data.Where(x => x.BaseAsset != x.QuoteAsset))
            {
                data.Add(new SymbolData
                {
                    BaseAsset               = symbol.BaseAsset,
                    QuoteAsset              = symbol.QuoteAsset,
                    Name                    = symbol.Name,
                    MaxTradeQuantity        = symbol.MaxTradeQuantity,
                    MinNotionalValue        = symbol.MinNotionalValue,
                    QuantityDecimals        = symbol.QuantityDecimals,
                    MinTradeQuantity        = symbol.MinTradeQuantity,
                    PriceDecimals           = symbol.PriceDecimals,
                    PriceSignificantFigures = symbol.PriceSignificantFigures,
                    PriceStep               = symbol.PriceStep,
                    QuantityStep            = symbol.QuantityStep,
                    Trading                 = symbol.Trading
                });
            }

            exchangeData.Data = data;
            await _publishOutput.PublishAsync(exchangeData);
        }
    }
}
