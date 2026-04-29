using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Interfaces;
using Shared.Infrastructure.API.ExceptionHandling;
using Shared.Infrastructure.API.ExceptionStrategies;

namespace Shared.Infrastructure.Extensions;

// Extension method to register exception handling services. Facade pattern for cleaner DI setup.
// Single responsibility - only handles exception handling service registration.
public static class ExceptionHandlingExtensions
{
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddSingleton<IExceptionResponseStrategy, UnauthorizedAccessExceptionStrategy>();
        services.AddSingleton<IExceptionResponseStrategy, ForbiddenExceptionStrategy>();
        services.AddSingleton<ExceptionHandler>();

        return services;
    }
}
