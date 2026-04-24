using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Domain.Entities;

[Table("OutboxMessages")]
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string OrderId { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; }
}