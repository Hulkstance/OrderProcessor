using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Behaviors;
using OrderService.Application.Orders.Services;

namespace OrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddScoped<OrderDomainService>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationErrorOrResultBehavior<,>));
            config.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });
        
        return services;
    }
}
