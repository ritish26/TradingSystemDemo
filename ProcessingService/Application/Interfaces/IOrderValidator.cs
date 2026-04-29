using Shared.Domain.Events;

namespace ProcessingService.Application.Interfaces;

/// <summary>
/// PURPOSE: Validate OrderPlacedEvent against business rules
///
/// WHY CREATED:
/// - Abstraction for validation (Dependency Inversion Principle)
/// - Enables multiple validation strategies without changing consumers
/// - Testable: Can mock validator in tests
/// - Reusable: Can be implemented by different services
/// - Separates validation logic from orchestration
///
/// SOLID PRINCIPLES APPLIED:
/// - Dependency Inversion (DIP): Depend on abstraction, not concrete OrderValidator
/// - Single Responsibility (SRP): Only validates, doesn't execute or publish
/// - Interface Segregation (ISP): Minimal, focused interface
///
/// STRATEGY PATTERN:
/// Different implementations can validate differently:
/// - BasicOrderValidator: Only checks required fields
/// - StrictOrderValidator: Includes market rules
/// - CustomValidator: Domain-specific rules
///
/// USAGE:
/// var result = validator.Validate(orderEvent);
/// if (!result.IsValid) {
///     logger.LogWarning($"Validation failed: {result.Message}");
/// }
///
/// BENEFIT:
/// Can inject different validators based on:
/// - Market conditions (strict vs lenient)
/// - Client tier (premium clients may have relaxed limits)
/// - Time of day (different rules for different hours)
/// - A/B testing (test new validation rules)
/// </summary>
public interface IOrderValidator
{
    /// <summary>
    /// Validates an order against business rules
    /// </summary>
    /// <param name="order">The order event to validate</param>
    /// <returns>Validation result with status and message</returns>
    OrderValidationResult Validate(OrderPlacedEvent order);

    /// <summary>
    /// Validates and throws if invalid
    /// </summary>
    /// <param name="order">The order event to validate</param>
    /// <exception cref="OrderValidationException">Thrown if validation fails</exception>
    void ValidateOrThrow(OrderPlacedEvent order);
}

/// <summary>
/// PURPOSE: Result object for validation
///
/// WHY CREATED:
/// - Encapsulates validation result
/// - Strongly typed alternative to Result<string, string>
/// - Clear API for success/failure cases
/// - Can carry additional context
///
/// USAGE:
/// var result = validator.Validate(order);
/// if (!result.IsValid) {
///     Console.WriteLine(result.ErrorMessage);
/// }
/// </summary>
public class OrderValidationResult
{
    public bool IsValid { get; private set; }
    public string Message { get; private set; }
    public string? ErrorCode { get; private set; }
    public Dictionary<string, string>? FailedRules { get; private set; }

    public OrderValidationResult(bool isValid, string message, string? errorCode = null, Dictionary<string, string>? failedRules = null)
    {
        IsValid = isValid;
        Message = message;
        ErrorCode = errorCode;
        FailedRules = failedRules;
    }

    public static OrderValidationResult Success(string message = "Order is valid") =>
        new(true, message);

    public static OrderValidationResult Failure(string message, string errorCode = "VALIDATION_ERROR", Dictionary<string, string>? failedRules = null) =>
        new(false, message, errorCode, failedRules);
}
