using Shared.Domain.Enum;
using Shared.Domain.Events;

namespace ProcessingService.Infrasturcture.Services;

/// <summary>
/// OrderValidator - Validates orders against business rules
/// </summary>
public class OrderValidator
{
    private readonly ILogger<OrderValidator> _logger;

    public OrderValidator(ILogger<OrderValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates an order based on business rules
    /// </summary>
    public ValidationResult Validate(OrderPlacedEvent order)
    {
        try
        {
            _logger.LogInformation($"Validating order {order.OrderId}");

            // Validate order structure
            if (string.IsNullOrEmpty(order.OrderId))
                return ValidationResult.Failure("OrderId is required");

            if (string.IsNullOrEmpty(order.ClientId))
                return ValidationResult.Failure("ClientId is required");

            if (string.IsNullOrEmpty(order.InstrumentSymbol))
                return ValidationResult.Failure("InstrumentSymbol is required");

            if (order.OrderType != OrderType.BUY && order.OrderType != OrderType.SELL)
                return ValidationResult.Failure("OrderType must be either BUY or SELL");

            // Validate quantity
            if (order.Quantity <= 0)
                return ValidationResult.Failure("Quantity must be greater than 0");

            // Validate price
            if (order.Price <= 0)
                return ValidationResult.Failure("Price must be greater than 0");

            // Business rule: Check order size limits
            var maxOrderSize = 100000; // Maximum units per order
            if (order.Quantity > maxOrderSize)
                return ValidationResult.Failure($"Order quantity exceeds maximum limit of {maxOrderSize}");

            // Business rule: Check total order value
            var maxOrderValue = 1000000m; // Maximum value in currency
            var totalValue = order.Quantity * order.Price;
            if (totalValue > maxOrderValue)
                return ValidationResult.Failure($"Order value exceeds maximum limit of {maxOrderValue}");

            // Business rule: Price sanity check
            // Prevent orders with unreasonable prices
            if (order.Price > 100000)
                return ValidationResult.Failure("Price exceeds reasonable limits");

            _logger.LogInformation($"Order {order.OrderId} passed validation");
            return ValidationResult.Success("Order is valid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating order {order.OrderId}");
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }

    public static ValidationResult Success(string message = "Valid")
    {
        return new ValidationResult { IsValid = true, Message = message };
    }

    public static ValidationResult Failure(string message)
    {
        return new ValidationResult { IsValid = false, Message = message };
    }
}