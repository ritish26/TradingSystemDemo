using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseInitialization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<DbInitializer>(); 

        return services;
    }
}

