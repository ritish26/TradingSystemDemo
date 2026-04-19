using Shared.Enum;

namespace OrderService2.Models;

public class OrderRequest
{
    public string? ClientId { get; set; }
    public string? InstrumentSymbol { get; set; }
    public OrderType OrderType { get; set; } // BUY or SELL
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}

