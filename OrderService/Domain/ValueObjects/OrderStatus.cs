namespace OrderService.Domain.ValueObjects;

/// <summary>
/// Typed constants for all valid order and outbox message statuses.
/// Eliminates magic strings scattered across the codebase.
/// </summary>
public static class OrderStatus
{
    public const string Pending   = "Pending";
    public const string Processed = "Processed";
    public const string Failed    = "Failed";
}