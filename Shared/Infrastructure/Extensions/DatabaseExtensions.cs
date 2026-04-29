using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Extensions;

// Extension method to register database services. Facade pattern for cleaner DI setup.
// Single responsibility - only handles database service registration.
public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabase, PostgreSqlDatabase>();
        return services;
    }
}
