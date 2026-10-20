using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Common.EventualConsistency;
using OrderService.Domain.Common.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Middleware;

public class EventualConsistencyMiddleware(RequestDelegate next)
{
    public const string DomainEventsKey = "DomainEventsKey";

    public async Task InvokeAsync(HttpContext context, IPublisher publisher, AppDbContext dbContext)
    {
        context.Response.OnCompleted(async () =>
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteInTransactionAsync(async () =>
            {
                await PublishEvents(context, publisher);
            }, null!);
        });

        await next(context);
    }

    private static async Task PublishEvents(HttpContext context, IPublisher publisher)
    {
        try
        {
            if (context.Items.TryGetValue(DomainEventsKey, out var value) &&
                value is Queue<IDomainEvent> domainEvents)
            {
                while (domainEvents.TryDequeue(out var nextEvent))
                    await publisher.Publish(nextEvent);
            }
        }
        // ReSharper disable once RedundantCatchClause
        catch (EventualConsistencyException)
        {
            // TODO: handle eventual consistency exception
            throw;
        }
    }
}
