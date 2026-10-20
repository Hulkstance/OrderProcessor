using CryptoExchange.Net.SharedApis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Orders.Services;
using OrderService.Infrastructure.PartnerIntegrations.Models;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class OrderUpdateProcessor(
    ILogger<OrderUpdateProcessor> logger,
    IProcessInput<OrderEvent> processInput,
    IServiceScopeFactory dbContextFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Starting OrderUpdateProcessor");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var input = await processInput.ReadAsync(stoppingToken);
            if (input != null)
                await ProcessAsync(input, stoppingToken);
        }

        logger.LogDebug("OrderUpdateProcessor stopped");
    }

    private async Task ProcessAsync(PublishItem<OrderEvent> update, CancellationToken ct)
    {
        await using var scope = dbContextFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var orderDomainService = scope.ServiceProvider.GetRequiredService<OrderDomainService>();

        var internalIds = update.Data.Select(e => e.ClientOrderId).ToList();

        var orders = await dbContext.Orders
            .Include(x => x.Fills)
            .Where(x => internalIds.Contains(x.InternalId))
            .ToListAsync(ct);

        foreach (var evt in update.Data)
        {
            var order = orders.FirstOrDefault(o => o.InternalId == evt.ClientOrderId);

            if (order is null)
            {
                logger.LogWarning("Received update for unknown order {ClientOrderId} on {Exchange} — skipping", evt.ClientOrderId, evt.Exchange);
                continue;
            }

            if (order.IsTerminalState())
                continue;

            switch (evt.Status)
            {
                case SharedOrderStatus.Open:
                    if (evt.QuantityFilledBase > 0)
                        orderDomainService.MarkOrderPartiallyFilled(order, evt.ExchangeOrderId, evt.QuantityFilledBase ?? 0m, evt.AveragePrice);
                    else
                        orderDomainService.MarkOrderWorking(order, evt.ExchangeOrderId);
                    break;

                case SharedOrderStatus.Filled:
                    orderDomainService.MarkOrderFilled(order, evt.ExchangeOrderId, evt.QuantityFilledBase ?? 0m, evt.AveragePrice);
                    break;

                case SharedOrderStatus.Canceled:
                    orderDomainService.MarkOrderCancelled(order);
                    break;

                default:
                    logger.LogWarning("Unknown exchange status '{Status}' for order {OrderId} — skipping", evt.Status, order.Id);
                    break;
            }
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogDebug("{Processor} flushed {Count} order update(s)", nameof(OrderUpdateProcessor), update.Data.Count());
    }
}
