using ProcessingService.Application.Interfaces;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Domain.ValueObjects;
using Shared.Domain.Enum;
using Shared.Domain.Events;

namespace ProcessingService.Infrastructure.Services;

/// <summary>
/// PURPOSE: Validate OrderPlacedEvent against business rules
///
/// WHY CREATED:
/// - Implementation of IOrderValidator interface
/// - Single responsibility: validates orders
/// - Encapsulates all validation rules in one place
/// - Reusable: can be injected into any service needing validation
/// - Testable: can be mocked or replaced with stub
/// - Maintainable: rules are clearly documented
///
/// SOLID PRINCIPLES ACHIEVED:
/// - Single Responsibility (SRP): Only validates, doesn't execute or publish
/// - Dependency Inversion (DIP): Implements IOrderValidator interface
/// - Open-Closed (OCP): Can add new validation rules without affecting consumers
///
/// VALIDATION STRATEGY:
/// Validates in layers:
/// 1. Structure validation - required fields present
/// 2. Type validation - types are correct
/// 3. Range validation - values within acceptable limits
/// 4. Business rule validation - order size, value limits
/// 5. Price sanity check - prevent unreasonable prices
///
/// EXTENSIBILITY:
/// In future, can:
/// - Inject dependencies for dynamic rule checking
/// - Load rules from database
/// - Support different rule sets per client tier
/// - Add rule engine for complex conditions
///
/// EXAMPLE USAGE:
/// var validator = new DefaultOrderValidator();
/// var result = validator.Validate(orderEvent);
/// if (!result.IsValid) {
///     logger.LogWarning($"Validation failed: {result.Message}");
/// }
///
/// BETTER USAGE (with exception handling):
/// var validator = new DefaultOrderValidator();
/// try {
///     validator.ValidateOrThrow(orderEvent);
///     // Proceed with execution
/// } catch (OrderValidationException ex) {
///     logger.LogWarning($"Order {ex.OrderId} validation failed: {ex.ValidationMessage}");
/// }
/// </summary>
public class DefaultOrderValidator : IOrderValidator
{
    private readonly ILogger<DefaultOrderValidator> _logger;

    public DefaultOrderValidator(ILogger<DefaultOrderValidator> logger)
    {
        _logger = logger;
    }

    public OrderValidationResult Validate(OrderPlacedEvent order)
    {
        try
        {
            _logger.LogInformation($"Validating order {order.OrderId}");

            // Validate order structure - required fields
            if (string.IsNullOrWhiteSpace(order.OrderId))
                return OrderValidationResult.Failure("OrderId is required", "MISSING_ORDER_ID");

            if (string.IsNullOrWhiteSpace(order.ClientId))
                return OrderValidationResult.Failure("ClientId is required", "MISSING_CLIENT_ID");

            if (string.IsNullOrWhiteSpace(order.InstrumentSymbol))
                return OrderValidationResult.Failure("InstrumentSymbol is required", "MISSING_INSTRUMENT");

            // Validate order type
            if (order.OrderType != OrderType.BUY && order.OrderType != OrderType.SELL)
                return OrderValidationResult.Failure(
                    $"OrderType must be either BUY or SELL, got {order.OrderType}",
                    "INVALID_ORDER_TYPE");

            // Validate quantity - must be positive and within limits
            if (order.Quantity <= ProcessingConstants.MinOrderQuantity)
                return OrderValidationResult.Failure(
                    $"Quantity must be greater than {ProcessingConstants.MinOrderQuantity}",
                    "INVALID_QUANTITY");

            if (order.Quantity > ProcessingConstants.MaxOrderQuantity)
                return OrderValidationResult.Failure(
                    $"Order quantity {order.Quantity} exceeds maximum limit of {ProcessingConstants.MaxOrderQuantity}",
                    "QUANTITY_EXCEEDS_LIMIT");

            // Validate price - must be positive and within limits
            if (order.Price < ProcessingConstants.MinOrderPrice)
                return OrderValidationResult.Failure(
                    $"Price must be greater than {ProcessingConstants.MinOrderPrice}",
                    "INVALID_PRICE");

            if (order.Price > ProcessingConstants.MaxUnitPrice)
                return OrderValidationResult.Failure(
                    $"Price {order.Price} exceeds reasonable limits of {ProcessingConstants.MaxUnitPrice}",
                    "PRICE_EXCEEDS_LIMIT");

            // Business rule: Check total order value
            var totalValue = (decimal)order.Quantity * order.Price;
            if (totalValue > ProcessingConstants.MaxOrderValue)
                return OrderValidationResult.Failure(
                    $"Order value {totalValue:C} exceeds maximum limit of {ProcessingConstants.MaxOrderValue:C}",
                    "ORDER_VALUE_EXCEEDS_LIMIT");

            _logger.LogInformation($"Order {order.OrderId} passed all validation checks");
            return OrderValidationResult.Success("Order is valid and ready for execution");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error validating order {order.OrderId}");
            return OrderValidationResult.Failure(
                $"Validation error: {ex.Message}",
                "VALIDATION_ERROR");
        }
    }

    public void ValidateOrThrow(OrderPlacedEvent order)
    {
        var result = Validate(order);
        if (!result.IsValid)
        {
            throw new OrderValidationException(result.Message, order.OrderId);
        }
    }
}
