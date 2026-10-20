using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Symbols;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using OrderService.Infrastructure.Persistence;
using System.Diagnostics;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class AssetCalculationProcessService(
    ILogger<AssetCalculationProcessService> logger,
    IProcessInput<PendingAssetCalculation> processInput,
    IServiceScopeFactory dbContextFactory)
    : BackgroundService
{
    private DateTime _lastLog;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Starting AssetCalculationService");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var input = await processInput.ReadAsync(stoppingToken);
            if (input != null)
                await ProcessAsync(input);
        }

        logger.LogDebug("AssetCalculationService stopped");
    }

    private async Task ProcessAsync(PublishItem<PendingAssetCalculation> update)
    {
        var sw = Stopwatch.StartNew();

        await using var scope = dbContextFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var ids = update.Data.Select(x => $"{x.Exchange}-{x.Asset}").ToList();
            var symbolData = await context.Set<ExchangeSymbol>()
                .Where(x => ids.Contains(x.BaseAssetExchangeId))
                .Select(x => new { x.Exchange, x.BaseAsset, x.BaseAssetType, x.QuoteAsset, x.QuoteAssetType, x.ChangePercentage, x.Volume, x.LastPrice, x.TickerType })
                .GroupBy(x => new { x.Exchange, x.BaseAsset })
                .ToListAsync();

            var data = symbolData.Select(x =>
            {
                var first = x.First();
                var asset = ExchangeAsset.Create(first.Exchange, first.BaseAsset, first.BaseAssetType);
                asset.UpdateTicker(
                    value:            x.Where(x => (x.QuoteAssetType == AssetType.Stable || x.QuoteAsset == "USD") && x.LastPrice != null).Average(x => x.LastPrice),
                    volume:           x.Sum(x => x.Volume ?? 0),
                    changePercentage: x.Where(x => (x.QuoteAssetType == AssetType.Stable || x.QuoteAsset == "USD") && x.ChangePercentage != null).Average(x => x.ChangePercentage),
                    tickerType:       first.TickerType);
                return asset;
            });

            await context.BulkInsertOrUpdateAsync(data, new BulkConfig { WithHoldlock = false });
            sw.Stop();

            if (DateTime.UtcNow - _lastLog > TimeSpan.FromMinutes(1))
            {
                _lastLog = DateTime.UtcNow;
                logger.LogInformation($"Exchange asset calculation done in {sw.ElapsedMilliseconds}ms for {update.Data.Count()} items");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in asset calculation");
        }
    }
}
