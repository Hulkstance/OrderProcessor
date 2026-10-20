using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Common.Behaviors;

public class LoggingBehavior<TRequest>(ILogger<TRequest> logger, ICurrentUserService currentUserService)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUserService.GetUserId();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("CleanArchitecture Request: {Name} {@UserId} {@Request}",
                requestName, userId, request);
        }

        return Task.CompletedTask;
    }
}
