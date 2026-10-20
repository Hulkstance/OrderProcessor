using BuildingBlocks.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Orders.Jobs;
using OrderService.Domain.Orders;

namespace OrderService.Application.Orders.EventHandlers;

public class OrderCancelRequestedDomainEventHandler(
    IJobRunner jobRunner,
    ILogger<OrderCancelRequestedDomainEventHandler> logger)
    : INotificationHandler<OrderCancelRequestedDomainEvent>
{
    public Task Handle(OrderCancelRequestedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Enqueuing CancelOrderJob for order {OrderId}",
                notification.OrderId);
        }

        jobRunner.Queue<CancelOrderJob, CancelOrderJobOptions>(
            new CancelOrderJobOptions(notification.OrderId));

        return Task.CompletedTask;
    }
}
