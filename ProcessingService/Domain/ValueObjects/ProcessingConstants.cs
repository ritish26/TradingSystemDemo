namespace ProcessingService.Domain.ValueObjects;

/// <summary>
/// PURPOSE: Centralize all configuration constants for the processing service
///
/// WHY CREATED:
/// - Eliminates magic strings scattered throughout codebase
/// - Single point of change for configuration (DRY principle)
/// - Easy to manage limits and business rules
/// - Improves readability and maintainability
///
/// SOLID PRINCIPLE APPLIED:
/// - Single Responsibility: This class only manages constants
/// - Open-Closed: Easy to extend with new configuration without modifying code
///
/// USAGE:
/// Inject or reference these constants across validators, executors, and services
/// Example: if (order.Quantity > ProcessingConstants.MaxOrderQuantity) ...
/// </summary>
public static class ProcessingConstants
{
    // RabbitMQ Configuration
    public const string OrderPlacedEventQueueName = "order-placed-events";
    public const string OrderExecutedEventQueueName = "order-executed-events";
    public const string OrderFailedEventQueueName = "order-failed-events";

    // Message Processing
    public const int QosPreFetchCount = 1; // Process one message at a time

    // Order Validation Limits
    /// <summary>Maximum number of units allowed per order</summary>
    public const int MaxOrderQuantity = 100000;

    /// <summary>Maximum total monetary value of an order</summary>
    public const decimal MaxOrderValue = 1000000m;

    /// <summary>Maximum reasonable price for a single unit</summary>
    public const decimal MaxUnitPrice = 100000m;

    /// <summary>Minimum order quantity</summary>
    public const int MinOrderQuantity = 1;

    /// <summary>Minimum order price</summary>
    public const decimal MinOrderPrice = 0.01m;

    // Market Data Configuration
    /// <summary>Maximum acceptable price variance from requested price (-1% to +1%)</summary>
    public const decimal MaxPriceVariance = 0.02m;

    // Execution Configuration
    /// <summary>Simulated execution latency (milliseconds)</summary>
    public const int ExecutionLatencyMs = 100;

    // Retry Configuration
    /// <summary>Maximum retries for failed operations</summary>
    public const int MaxRetries = 3;

    /// <summary>Delay between retries (milliseconds)</summary>
    public const int RetryDelayMs = 100;
}
