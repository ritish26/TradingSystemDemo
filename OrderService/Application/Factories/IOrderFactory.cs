using OrderService.Application.Command;
using OrderService.Domain.Entities;

namespace OrderService.Application.Factories;

/// <summary>
/// Factory interface for creating Order aggregate and its associated OutboxMessage.
/// Keeps command handlers free of entity construction details (SRP).
/// </summary>
public interface IOrderFactory
{
    Order CreateOrder(OrderCreatedCommand command);
    OutboxMessage CreateOutboxMessage(OrderCreatedCommand command);
}
