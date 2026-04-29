using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Extensions;

// Facade for all shared infrastructure services. Single entry point for DI registration.
// Calls specialized extension methods for each service area (Configuration, Database, Messaging, ExceptionHandling).
// Provides clean, organized dependency injection setup.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddConfigurationProvider();
        services.AddDatabaseServices();
        services.AddMessagingServices();
        services.AddExceptionHandling();

        return services;
    }
}
