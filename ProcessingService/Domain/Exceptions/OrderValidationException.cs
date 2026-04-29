namespace ProcessingService.Domain.Exceptions;

/// <summary>
/// PURPOSE: Represent validation failures at the domain level
///
/// WHY CREATED:
/// - Separates validation errors from other system exceptions
/// - Allows handlers to distinguish between "invalid order" vs "system error"
/// - Better error handling strategy (validation vs infrastructure issues)
/// - Domain-driven design: exception as part of domain model
///
/// SOLID PRINCIPLE APPLIED:
/// - Single Responsibility: This exception represents validation failures only
/// - Open-Closed: Can be extended with domain-specific validation context
///
/// USAGE:
/// throw new OrderValidationException("ClientId is required", order);
/// OR
/// catch (OrderValidationException ex) { ... }
///
/// BENEFIT:
/// Instead of:
///   var result = validator.Validate(order);
///   if (!result.IsValid) { ... } // Result pattern
///
/// We can now use:
///   try {
///       validator.ValidateOrThrow(order);
///   } catch (OrderValidationException ex) {
///       // Clear, type-safe error handling
///   }
/// </summary>
public class OrderValidationException : DomainException
{
    public string ValidationMessage { get; }
    public string? OrderId { get; }

    public OrderValidationException(string message, string? orderId = null)
        : base($"Order validation failed: {message}")
    {
        ValidationMessage = message;
        OrderId = orderId;
    }
}

/// <summary>
/// PURPOSE: Base exception for all domain-level errors
///
/// WHY CREATED:
/// - Distinguish domain exceptions from infrastructure/system exceptions
/// - Allows catching all domain errors: catch (DomainException ex)
/// - Part of domain-driven design approach
/// - Makes exception hierarchy clear and organized
///
/// BENEFIT:
/// try {
///     order.Validate();  // Can throw any DomainException
/// } catch (DomainException ex) {
///     // Handle domain-level errors
/// } catch (Exception ex) {
///     // Handle infrastructure errors
/// }
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
