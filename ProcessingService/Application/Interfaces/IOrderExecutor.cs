using Shared.Domain.Events;

namespace ProcessingService.Application.Interfaces;

/// <summary>
/// PURPOSE: Execute validated orders in the trading system
///
/// WHY CREATED:
/// - Abstraction for order execution (Dependency Inversion)
/// - Separates execution logic from orchestration
/// - Enables different execution strategies
/// - Testable: Mock execution in unit tests
/// - Reusable: Multiple services can use same executor interface
///
/// SOLID PRINCIPLES APPLIED:
/// - Dependency Inversion (DIP): Depend on IOrderExecutor, not OrderExecutor
/// - Single Responsibility (SRP): Only executes, doesn't validate or publish
/// - Interface Segregation (ISP): Minimal, focused interface
///
/// STRATEGY PATTERN:
/// Different implementations can execute differently:
/// - StandardExecutor: Executes at market price
/// - LimitOrderExecutor: Executes only at specified price
/// - TimeWeightedExecutor: Distributes execution over time
/// - VolumeWeightedExecutor: Executes proportional to volume
///
/// USAGE:
/// var result = executor.Execute(validatedOrder);
/// if (result.IsSuccessful) {
///     logger.LogInformation($"Order executed: {result.ExecutionId}");
/// }
///
/// DEPENDENCY INJECTION:
/// The executor may depend on:
/// - IMarketDataProvider (get prices, check market conditions)
/// - IClientPositionProvider (check if client can sell)
/// - IOrderExecutionEventPublisher (publish execution event)
/// These are injected via constructor, not hardcoded
///
/// BENEFIT:
/// Can swap executors based on:
/// - Order type (market vs limit)
/// - Client tier (VIP vs standard)
/// - Market conditions (fast vs slow execution)
/// - Risk management (conservative vs aggressive)
/// </summary>
public interface IOrderExecutor
{
    /// <summary>
    /// Executes an order in the trading system
    /// </summary>
    /// <param name="order">The validated order to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result with ID and status</returns>
    Task<OrderExecutionResult> ExecuteAsync(OrderPlacedEvent order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an order or throws if execution fails
    /// </summary>
    /// <param name="order">The validated order to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="OrderExecutionException">Thrown if execution fails</exception>
    Task<OrderExecutionResult> ExecuteOrThrowAsync(OrderPlacedEvent order, CancellationToken cancellationToken = default);
}

/// <summary>
/// PURPOSE: Result object for order execution
///
/// WHY CREATED:
/// - Encapsulates execution result and metadata
/// - Carries execution ID for tracking
/// - Distinguishes success from failure cases
/// - Can carry execution details for audit trail
///
/// USAGE:
/// var result = await executor.ExecuteAsync(order);
/// if (result.IsSuccessful) {
///     // Log execution ID for tracking
///     auditLog.Record($"Order executed: {result.ExecutionId}");
/// } else {
///     // Log failure reason for investigation
///     logger.LogError($"Execution failed: {result.ErrorMessage}");
/// }
/// </summary>
public class OrderExecutionResult
{
    public bool IsSuccessful { get; private set; }
    public string? ExecutionId { get; private set; }
    public string Message { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public Dictionary<string, object>? ExecutionDetails { get; private set; }

    private OrderExecutionResult(
        bool isSuccessful,
        string message,
        string? executionId = null,
        string? errorMessage = null,
        Dictionary<string, object>? executionDetails = null)
    {
        IsSuccessful = isSuccessful;
        Message = message;
        ExecutionId = executionId;
        ErrorMessage = errorMessage;
        ExecutedAt = DateTime.UtcNow;
        ExecutionDetails = executionDetails;
    }

    /// <summary>
    /// Create successful execution result
    /// </summary>
    public static OrderExecutionResult Success(
        string executionId,
        string message,
        Dictionary<string, object>? details = null) =>
        new(true, message, executionId, null, details);

    /// <summary>
    /// Create failed execution result
    /// </summary>
    public static OrderExecutionResult Failure(
        string errorMessage,
        string message = "Execution failed",
        Dictionary<string, object>? details = null) =>
        new(false, message, null, errorMessage, details);
}
