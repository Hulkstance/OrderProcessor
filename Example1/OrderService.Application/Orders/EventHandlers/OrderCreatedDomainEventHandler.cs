using BuildingBlocks.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Orders.Jobs;
using OrderService.Domain.Orders;

namespace OrderService.Application.Orders.EventHandlers;

public class OrderCreatedDomainEventHandler(
    IJobRunner jobRunner,
    ILogger<OrderCreatedDomainEventHandler> logger)
    : INotificationHandler<OrderCreatedDomainEvent>
{
    public Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Enqueuing CreateOrderJob for order {OrderId}",
                notification.OrderId);
        }

        jobRunner.Queue<CreateOrderJob, CreateOrderJobOptions>(
            new CreateOrderJobOptions(notification.OrderId));

        return Task.CompletedTask;
    }
}
