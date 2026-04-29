using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseInitialization(this IServiceCollection services)
    {
        services.AddSingleton<IDatabase, PostgreSqlDatabase>();
        return services;
    }
}

