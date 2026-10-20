using EFCore.BulkExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Symbols;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class TickerProcessService(
    ILogger<TickerProcessService> logger,
    IProcessInput<TickerData> processInput,
    IPublishOutput<PendingAssetCalculation> assetCalcOutput,
    IServiceScopeFactory dbContextFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Starting TickerProcessService");

        while (!stoppingToken.IsCancellationRequested)
        {
            var input = await processInput.ReadAsync(stoppingToken);
            if (input != null)
                await ProcessAsync(input);
        }

        logger.LogDebug("TickerProcessService stopped");
    }

    private async Task ProcessAsync(PublishItem<TickerData> update)
    {
        await using var scope = dbContextFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var symbols = new List<ExchangeSymbol>(update.Data.Count());
        foreach (var item in update.Data)
        {
            var lastPrice = ClampValue(item.LastPrice);
            var highPrice = ClampValue(item.HighPrice);
            var lowPrice = ClampValue(item.LowPrice);
            var volume = ClampValue(item.Volume);
            var quoteVolume = ClampValue(item.QuoteVolume);
            var changePercentage = item.ChangePercentage;

            // Try to derive some data if it's not returned by the API
            if (quoteVolume == null)
            {
                if (lowPrice.HasValue && highPrice.HasValue)
                    // Take average of high/low price as avg price
                    quoteVolume = (lowPrice.Value + highPrice.Value) / 2 * volume;
                if (changePercentage.HasValue)
                    // Take half of change percentage as avg price
                    quoteVolume = (lastPrice * Math.Abs((changePercentage.Value / 200) - 1)) * volume;
                else
                    // Fallback to last price as avg price
                    quoteVolume = lastPrice * volume;
            }

            if (volume == null)
            {
                if (lastPrice != null)
                    // Take the quote asset divided by the last price. Not very accurate..
                    volume = quoteVolume / lastPrice;
            }

            var symbol = ExchangeSymbol.ForTickerUpdate(
                id:               $"{item.Exchange}-{item.BaseAsset.ToUpperInvariant()}-{item.QuoteAsset.ToUpperInvariant()}",
                lastPrice:        lastPrice,
                highPrice:        highPrice,
                lowPrice:         lowPrice,
                volume:           volume,
                quoteVolume:      quoteVolume,
                changePercentage: changePercentage,
                tickerType:       item.TickerType);

            symbols.Add(symbol);
        }

        try
        {
            await context.BulkUpdateAsync(symbols, new BulkConfig
            {
                PropertiesToInclude =
                [
                    nameof(ExchangeSymbol.Id),
                    nameof(ExchangeSymbol.ChangePercentage),
                    nameof(ExchangeSymbol.LastPrice),
                    nameof(ExchangeSymbol.HighPrice),
                    nameof(ExchangeSymbol.LowPrice),
                    nameof(ExchangeSymbol.Volume),
                    nameof(ExchangeSymbol.QuoteVolume),
                    nameof(ExchangeSymbol.LastSyncedAt),
                    nameof(ExchangeSymbol.TickerType)
                ],
                WithHoldlock = false
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Ticker update");
        }

        await assetCalcOutput.PublishAsync(new PublishItem<PendingAssetCalculation>
        {
            Data = update.Data.Select(x => new PendingAssetCalculation { Exchange = x.Exchange, Asset = x.BaseAsset })
        });
    }

    private static decimal? ClampValue(decimal? value)
    {
        if (value == null)
            return null;

        if (value > 99999999999999999999m)
            value = 99999999999999999999m;

        return Math.Round(value.Value, 8);
    }
}
