using FluentValidation;
using OrderService.Application.Command;
using OrderService.Application.Command.Mapper;
using OrderService.Application.Factories;
using OrderService.Application.Interfaces;
using OrderService.AuthorizationRegistry;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Service;
using OrderService.Outbox;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Messaging;

namespace OrderService.Infrastructure.Extensions;

/// <summary>
/// Facade pattern for Dependency Injection configuration.
/// Encapsulates all service registrations in a single extension method.
/// Benefits:
/// - Program.cs stays clean and readable
/// - All DI logic is centralized and reusable
/// - Easy to enable/disable service groups
/// - Single point of change for dependency configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Order Service dependencies.
    /// This is a Facade method that hides the complexity of multiple service registrations.
    /// </summary>
    public static IServiceCollection AddOrderServiceDependencies(this IServiceCollection services)
    {
        // Repository Layer
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Application Services
        services.AddScoped<IOrderService, OrderService.Infrastructure.Service.OrderService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Outbox Processing
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxBackgroundService>();

        // MediatR - CQRS Pattern
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderCreatedCommandHandler>();
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        });

        // Event Publishing
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // Factories
        services.AddSingleton<IOrderFactory, OrderFactory>();

        // Authorization
        services.AddSingleton<ICommandAuthorizationRegistry, CommandAuthorizationRegistry>();

        // AutoMapper - Object Mapping
        services.AddAutoMapper(typeof(OrderMappingProfile));

        // FluentValidation - Input Validation
        services.AddValidatorsFromAssemblyContaining<OrderCreatedCommandValidator>();

        return services;
    }
}
