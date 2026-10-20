using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderService.Application.Common.Behaviors;

public class UnhandledExceptionBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "CleanArchitecture Request: Unhandled Exception for Request {Name} {@Request}",
                    requestName, request);
            }

            throw;
        }
    }
}
