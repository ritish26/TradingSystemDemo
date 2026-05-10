using System.ComponentModel.DataAnnotations.Schema;
using Shared.Domain.Enum;

namespace OrderService.Domain.Entities;

[Table("Orders")]
public class Order
{
    public string OrderId { get; set; }
    public string? ClientId { get; set; }
    public required string InstrumentId { get; set; }
    public OrderType OrderType { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; }
    public uint xmin { get; set; }
}