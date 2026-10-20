using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using System.Diagnostics;

namespace OrderService.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(
    ILogger<TRequest> logger,
    ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next(cancellationToken);

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = currentUserService.GetUserId();

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "CleanArchitecture Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@UserId} {@Request}",
                    requestName, elapsedMilliseconds, userId, request);
            }
        }

        return response;
    }
}
