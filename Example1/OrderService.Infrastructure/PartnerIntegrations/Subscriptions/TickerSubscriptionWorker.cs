using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Symbols;
using OrderService.Infrastructure.PartnerIntegrations.Models;

namespace OrderService.Infrastructure.PartnerIntegrations.Subscriptions;

internal sealed class TickerSubscriptionWorker : BackgroundService
{
    private const string ConnectionId = "server";

    private readonly ILogger<TickerSubscriptionWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TickerSubscriptionService _tickerSubscriptionService;
    private readonly IPublishOutput<TickerData> _tickerOutput;

    public TickerSubscriptionWorker(
        ILogger<TickerSubscriptionWorker> logger,
        IServiceScopeFactory scopeFactory,
        TickerSubscriptionService tickerSubscriptionService,
        IPublishOutput<TickerData> tickerOutput)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _tickerSubscriptionService = tickerSubscriptionService;
        _tickerOutput = tickerOutput;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var symbols = await dbContext.ExchangeSymbols
            .Where(s => s.Enabled == true && s.DeletedAt == null)
            .AsNoTracking()
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Subscribing to ticker updates for {Count} symbol(s)", symbols.Count);

        foreach (var symbol in symbols)
        {
            var result = await _tickerSubscriptionService.SubscribeAsync(
                ConnectionId,
                symbol.Id,
                update => ProcessUpdate(symbol.Id, update),
                evnt => _logger.LogInformation(
                    "Ticker {SymbolId} stream {Status}",
                    symbol.Id, evnt.Status),
                stoppingToken);

            if (!result.Success)
                _logger.LogWarning("Failed to subscribe ticker for {SymbolId}: {Error}", symbol.Id, result.Error?.ToString());
        }

        _logger.LogInformation("Ticker subscription seeding complete");

        // Keep alive until stopped
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
    }

    private void ProcessUpdate(string symbolId, DataEvent<SharedSpotTicker> update)
    {
        var symbolData = symbolId.Split("-");
        var tickerData = new TickerData(
            symbolData[0],
            update.Data.SharedSymbol?.BaseAsset ?? symbolData[1],
            update.Data.SharedSymbol?.QuoteAsset ?? symbolData[2],
            update.Data.LastPrice,
            update.Data.HighPrice,
            update.Data.LowPrice,
            update.Data.Volume,
            update.Data.QuoteVolume,
            update.Data.ChangePercentage,
            TickerType.Other);

        _ = _tickerOutput.PublishAsync(new PublishItem<TickerData> { Data = [tickerData] });
    }
}
