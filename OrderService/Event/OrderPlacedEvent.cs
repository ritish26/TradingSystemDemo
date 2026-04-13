namespace OrderService2.Event;

public class OrderPlacedEvent
{
    public string? OrderId { get; set; }
    public string? ClientId { get; set; }
    public string? InstrumentSymbol { get; set; }
    public string? OrderType { get; set; } // BUY or SELL
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string? Status { get; set; } = "PLACED";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

