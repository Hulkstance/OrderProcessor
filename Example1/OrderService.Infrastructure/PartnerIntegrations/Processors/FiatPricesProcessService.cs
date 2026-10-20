using EFCore.BulkExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Symbols;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class FiatPricesProcessService(
    ILogger<FiatPricesProcessService> logger,
    IProcessInput<FiatPriceData> processInput,
    IServiceScopeFactory dbContextFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Starting FiatPricesProcessService");

        while (!stoppingToken.IsCancellationRequested)
        {
            var input = await processInput.ReadAsync(stoppingToken);
            if (input != null)
                await ProcessAsync(input);
        }

        logger.LogDebug("FiatPricesProcessService stopped");
    }

    private async Task ProcessAsync(PublishItem<FiatPriceData> update)
    {
        await using var scope = dbContextFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fiatPrices = update.Data
            .Select(item => FiatPrice.Create(item.Currency, item.Price))
            .ToList();

        try
        {
            await context.BulkInsertOrUpdateAsync(fiatPrices, new BulkConfig { WithHoldlock = false });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Fiat prices update");
        }
    }
}
