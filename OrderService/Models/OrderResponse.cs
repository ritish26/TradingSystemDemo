namespace OrderService.Models;

public class OrderResponse(string? orderId, string status, string message)
{
    public string? OrderId { get; set; } = orderId;
    public string Status { get; set; } = status;
    public string Message { get; set; } = message;
}