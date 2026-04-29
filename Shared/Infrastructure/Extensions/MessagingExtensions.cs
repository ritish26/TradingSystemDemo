using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Messaging;

namespace Shared.Infrastructure.Extensions;

// Extension method to register messaging services. Facade pattern for cleaner DI setup.
// Single responsibility - only handles messaging service registration.
public static class MessagingExtensions
{
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        services.AddSingleton<IMessagingConnection, RabbitMqConnection>();
        return services;
    }
}
