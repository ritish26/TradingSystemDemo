namespace OrderService2.Model;

public class OrderRequest
{
    public string? ClientId { get; set; }
    public string? InstrumentSymbol { get; set; }
    public string? OrderType { get; set; } // BUY or SELL
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}

