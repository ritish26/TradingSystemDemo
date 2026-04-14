namespace ProcessingService.Events;

public class OrderProcessedEvent
{
    public string? OrderId { get; set; }
    public string? ClientId { get; set; }
    public string? InstrumentSymbol { get; set; }
    public string? Status { get; set; } // EXECUTED, REJECTED, FAILED
    public string? Message { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

