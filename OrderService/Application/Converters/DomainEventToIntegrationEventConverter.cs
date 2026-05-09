using OrderService.Domain.Events;
using Shared.Domain.Events;

namespace OrderService.Application.Converters;

public static class DomainEventToIntegrationEventConverter
{
    public static OrderPlacedEvent Convert(OrderCreated domainEvent)
    {
        return new OrderPlacedEvent
        {
            OrderId = domainEvent.OrderId,
            ClientId = domainEvent.ClientId,
            InstrumentSymbol = domainEvent.InstrumentId,
            Quantity = domainEvent.Quantity,
            Price = domainEvent.Price,
            CreatedAt = domainEvent.CreatedAt,
            Status = "PLACED"
        };
    }
}
