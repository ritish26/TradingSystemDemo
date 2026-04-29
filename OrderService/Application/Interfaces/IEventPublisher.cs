using Shared.Domain.Events;

namespace OrderService.Application.Interfaces;

/// <summary>
/// Strategy abstraction for publishing domain events.
/// Swap RabbitMQ for Kafka / Azure Service Bus by providing a new implementation.
/// </summary>
public interface IEventPublisher
{
    Task PublishOrderPlacedEventAsync(OrderPlacedEvent orderEvent);
}