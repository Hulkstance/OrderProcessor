using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Symbols;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class SymbolUsdVolumeService(
    ILogger<SymbolUsdVolumeService> logger,
    IServiceScopeFactory dbContextFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessAsync();
            try { await Task.Delay(5000, stoppingToken); } catch { }
        }
    }

    private async Task ProcessAsync()
    {
        try
        {
            await using var scope = dbContextFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Get fiat prices
            var fiatPrices = await context.FiatPrices.ToListAsync();

            // Get all symbols we need to process
            var allSymbolVolumes = await context.ExchangeSymbols
                .Select(x => new { x.Id, x.Exchange, x.QuoteAsset, x.QuoteVolume })
                .ToListAsync();

            // Get all exchange quote asset prices
            var quoteSymbols = allSymbolVolumes
                .GroupBy(x => new { x.Exchange, x.QuoteAsset })
                .Select(x => $"{x.Key.Exchange}-{x.Key.QuoteAsset}")
                .ToList();

            // Get all quote asset exchange prices we need
            var quoteSymbolPrices = await context.ExchangeAssets
                .Where(x => quoteSymbols.Contains(x.Id))
                .Select(x => new { x.Id, x.Value })
                .ToListAsync();

            var data = new List<ExchangeSymbol>(allSymbolVolumes.Count);
            foreach (var symbol in allSymbolVolumes)
            {
                // If the price of the quote asset is known/traded on the exchange itself, use that
                var quotePrice = quoteSymbolPrices.SingleOrDefault(x => x.Id == symbol.Exchange + "-" + symbol.QuoteAsset)?.Value;
                if (quotePrice == null)
                {
                    // If the quote asset is a fiat currency use the exchange rate for that
                    var fiatPrice = fiatPrices.SingleOrDefault(x => x.Id == symbol.QuoteAsset);
                    if (fiatPrice != null)
                    {
                        quotePrice = 1 / fiatPrice.Price;
                    }
                    else
                    {
                        // If there are other exchanges trading this quote asset use the average price from those
                        var otherExchangePrices = quoteSymbolPrices
                            .Where(x => x.Value != null && x.Id.EndsWith($"-{symbol.QuoteAsset}"))
                            .ToList();
                        if (otherExchangePrices.Count == 0)
                            continue;

                        quotePrice = otherExchangePrices.Average(x => x.Value);
                    }
                }

                data.Add(ExchangeSymbol.ForUsdVolumeUpdate(symbol.Id, symbol.QuoteVolume * quotePrice!.Value));
            }

            await context.BulkInsertOrUpdateAsync(data, new BulkConfig
            {
                PropertiesToInclude =
                [
                    nameof(ExchangeSymbol.Id),
                    nameof(ExchangeSymbol.UsdVolume),
                    nameof(ExchangeSymbol.LastSyncedAt)
                ],
                WithHoldlock = false
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Symbols USD volume calculation");
        }
    }
}
