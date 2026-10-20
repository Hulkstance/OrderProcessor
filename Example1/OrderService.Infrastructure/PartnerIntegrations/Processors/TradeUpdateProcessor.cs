using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.PartnerIntegrations.Models;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class TradeUpdateProcessor(
    ILogger<TradeUpdateProcessor> logger,
    IProcessInput<TradeEvent> processInput)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Starting TradeUpdateProcessor");
        while (!stoppingToken.IsCancellationRequested)
        {
            var input = await processInput.ReadAsync(stoppingToken);
            if (input != null)
                await ProcessAsync(input, stoppingToken);
        }

        logger.LogDebug("TradeUpdateProcessor stopped");
    }

    private Task ProcessAsync(PublishItem<TradeEvent> update, CancellationToken ct)
        => Task.CompletedTask;
}
