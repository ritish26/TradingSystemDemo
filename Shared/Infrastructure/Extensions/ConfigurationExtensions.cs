using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Configuration;

namespace Shared.Infrastructure.Extensions;

// Extension method to register configuration services. Facade pattern for cleaner DI setup.
// Single responsibility - only handles configuration service registration.
public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationProvider(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
        return services;
    }
}
