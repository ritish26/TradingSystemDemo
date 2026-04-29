using ProcessingService.Application.Interfaces;
using ProcessingService.Infrastructure.Messaging;
using ProcessingService.Infrastructure.Providers;
using ProcessingService.Infrastructure.Publishing;
using ProcessingService.Infrastructure.Services;
using Shared.Infrastructure.Messaging;

namespace ProcessingService.Infrastructure.Extensions;

/// <summary>
/// PURPOSE: Facade pattern for Dependency Injection configuration
///
/// WHY CREATED:
/// - Centralizes all service registrations in one place
/// - Keeps Program.cs clean and readable
/// - Single point of change for dependency configuration
/// - Reusable across multiple projects/contexts
/// - Easy to enable/disable features
/// - Clear intent: "Add processing service dependencies"
///
/// SOLID PRINCIPLES APPLIED:
/// - Single Responsibility (SRP): Only registers dependencies
/// - Open-Closed (OCP): Easy to add new service registrations
/// - Dependency Inversion (DIP): All registrations depend on abstractions
///
/// FACADE PATTERN:
/// Hide complexity of 10+ service registrations behind single method:
/// builder.Services.AddProcessingServiceDependencies();
///
/// vs
///
/// builder.Services.AddSingleton<IOrderValidator, DefaultOrderValidator>();
/// builder.Services.AddSingleton<IOrderExecutor, DefaultOrderExecutor>();
/// builder.Services.AddSingleton<IMarketDataProvider, MarketDataProvider>();
/// ... (many more lines)
///
/// USAGE IN PROGRAM.CS:
/// builder.Services.AddProcessingServiceDependencies();
///
/// BENEFIT:
/// - Program.cs remains clean and focused
/// - DI configuration is organized and findable
/// - Changes to dependencies require update only here
/// - Can be tested independently
/// - Easily copied to other services for consistency
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Processing Service dependencies
    /// Facade method that hides complexity of multiple service registrations
    /// </summary>
    public static IServiceCollection AddProcessingServiceDependencies(
        this IServiceCollection services)
    {
        // Messaging - RabbitMQ connection and consumers
        services.AddSingleton<RabbitMqConnection>();
        services.AddScoped<IOrderValidator, DefaultOrderValidator>();
        services.AddScoped<IOrderExecutor, DefaultOrderExecutor>();
        services.AddScoped<IMarketDataProvider, MarketDataProvider>();
        services.AddScoped<IClientPositionProvider, ClientPositionProvider>();
        services.AddScoped<IOrderExecutionEventPublisher, OrderExecutionEventPublisher>();

        // Event consumers
        services.AddScoped<OrderPlacedEventConsumer>();

        // Background service for message consumption
        services.AddHostedService<RabbitConsumerService>();

        return services;
    }
}
