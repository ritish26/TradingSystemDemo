using System.Text.Json;
using OrderService.Application.Command;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Factories;

/// <summary>
/// Factory for constructing Order aggregates and outbox messages.
/// Centralizes entity creation logic, ensuring consistency and SRP.
/// Outbox stores domain events (OrderCreated), not integration events.
/// </summary>
public class OrderFactory : IOrderFactory
{
    private static readonly string OrderCreatedEventType = nameof(OrderCreated);

    public Order CreateOrder(OrderCreatedCommand command)
    {
        return new Order
        {
            OrderId = command.OrderId,
            ClientId = command.ClientId,
            InstrumentId = command.InstrumentSymbol,
            OrderType = command.OrderType,
            Quantity = command.Quantity,
            Price = command.Price,
            CreatedAt = command.CreatedAt,
            Status = OrderStatus.Pending
        };
    }

    public OutboxMessage CreateOutboxMessage(OrderCreatedCommand command)
    {
        var domainEvent = new OrderCreated(
            command.OrderId,
            command.ClientId,
            command.InstrumentSymbol,
            command.Quantity,
            command.Price,
            command.CreatedAt
        );

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OrderId = command.OrderId,
            EventType = OrderCreatedEventType,
            Payload = JsonSerializer.Serialize(domainEvent),
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };
    }
}
