namespace OrderService.Domain.Events;

public class OrderCreated
{
    public string OrderId { get; init; }
    public string ClientId { get; init; }
    public string InstrumentId { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }

    public OrderCreated(string orderId, string clientId, string instrumentId, int quantity, decimal price, DateTime createdAt)
    {
        OrderId = orderId;
        ClientId = clientId;
        InstrumentId = instrumentId;
        Quantity = quantity;
        Price = price;
        CreatedAt = createdAt;
    }
}
