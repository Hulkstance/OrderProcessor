using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.PartnerIntegrations.Models;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class BalanceUpdateProcessor(
    ILogger<BalanceUpdateProcessor> logger,
    IProcessInput<BalanceEvent> processInput)
    : BackgroundService
{
    private CancellationToken _stoppingToken;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        logger.LogDebug("Starting BalanceUpdateProcessor");
        while (!_stoppingToken.IsCancellationRequested)
        {
            var input = await processInput.ReadAsync(stoppingToken);
            if (input != null)
                await ProcessAsync(input, stoppingToken);
        }

        logger.LogDebug("BalanceUpdateProcessor stopped");
    }

    private Task ProcessAsync(PublishItem<BalanceEvent> update, CancellationToken ct)
        => Task.CompletedTask;
}
