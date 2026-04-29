using System.Text.Json;
using OrderService.Application.Command;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Factories;

/// <summary>
/// Factory for constructing Order aggregates and outbox messages.
/// Centralizes entity creation logic, ensuring consistency and SRP.
/// </summary>
public class OrderFactory : IOrderFactory
{
    private static readonly string OrderCreatedEvent = "OrderCreatedEvent";

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
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OrderId = command.OrderId,
            EventType = OrderCreatedEvent,
            Payload = JsonSerializer.Serialize(command),
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };
    }
}
