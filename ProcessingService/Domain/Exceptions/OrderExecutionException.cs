namespace ProcessingService.Domain.Exceptions;

/// <summary>
/// PURPOSE: Represent execution failures at the domain level
///
/// WHY CREATED:
/// - Separate execution failures from validation failures
/// - Distinguish from infrastructure errors
/// - Enables type-safe error handling
/// - Domain-driven design: errors are first-class domain concepts
///
/// SOLID PRINCIPLE APPLIED:
/// - Single Responsibility: Represents execution errors only
/// - Open-Closed: Can extend with execution context (market conditions, positions, etc.)
///
/// USAGE:
/// throw new OrderExecutionException("Insufficient inventory", orderId, "INVENTORY_ERROR");
/// OR
/// catch (OrderExecutionException ex) {
///     logger.LogError($"Execution failed for {ex.OrderId}: {ex.ErrorCode}");
/// }
///
/// BENEFIT:
/// Allows fine-grained error handling:
/// try {
///     executor.ExecuteOrThrow(order);
/// } catch (OrderValidationException vex) {
///     // Invalid order - reject it
/// } catch (OrderExecutionException eex) {
///     // Execution failed - retry or handle based on error code
/// } catch (DomainException dex) {
///     // Unknown domain error
/// } catch (Exception ex) {
///     // Infrastructure error - log and retry
/// }
/// </summary>
public class OrderExecutionException : DomainException
{
    public string ErrorCode { get; }
    public string? OrderId { get; }
    public string ExecutionDetails { get; }

    public OrderExecutionException(
        string message,
        string? orderId = null,
        string errorCode = "EXECUTION_ERROR",
        string executionDetails = "")
        : base($"Order execution failed: {message}")
    {
        ErrorCode = errorCode;
        OrderId = orderId;
        ExecutionDetails = executionDetails;
    }

    /// <summary>
    /// Create exception for market conditions
    /// </summary>
    public static OrderExecutionException MarketNotSuitable(string? orderId, string reason) =>
        new("Market conditions not suitable for trading", orderId, "MARKET_UNSUITABLE", reason);

    /// <summary>
    /// Create exception for insufficient inventory
    /// </summary>
    public static OrderExecutionException InsufficientInventory(string? orderId, string instrument) =>
        new("Insufficient inventory", orderId, "INVENTORY_ERROR", $"Instrument: {instrument}");

    /// <summary>
    /// Create exception for insufficient client position
    /// </summary>
    public static OrderExecutionException InsufficientPosition(string? orderId, string clientId, string instrument) =>
        new("Client does not have sufficient position", orderId, "POSITION_ERROR",
            $"Client: {clientId}, Instrument: {instrument}");
}
