using MediatR;
using Shared.Enum;

namespace OrderService.Command;

public class OrderCreatedCommand : IRequest<Unit>
{
    public string? OrderId { get; set; }
    public string? ClientId { get; set; }
    public string? InstrumentSymbol { get; set; }
    public OrderType OrderType { get; set; } 
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

