using System.Text.Json;
using ProcessingService.Infrastructure.Services;
using ProcessingService.Infrasturcture.Services;
using Shared.Domain.Events;

namespace ProcessingService.Infrastructure.Messaging;

/// <summary>
/// OrderPlacedConsumer - Processes OrderPlaced events from the queue
/// </summary>
public class OrderPlacedConsumer
{
    private readonly OrderValidator _orderValidator;
    private readonly OrderExecutor _orderExecutor;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(
        OrderValidator orderValidator,
        OrderExecutor orderExecutor,
        ILogger<OrderPlacedConsumer> logger)
    {
        _orderValidator = orderValidator;
        _orderExecutor = orderExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Processes an OrderPlacedEvent
    /// </summary>
    public async Task ConsumeAsync(string message)
    {
        try
        {
            _logger.LogInformation($"Received message: {message}");

            // Deserialize the event
            var orderEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(message);
            if (orderEvent == null)
            {
                _logger.LogError("Failed to deserialize OrderPlacedEvent from message");
                return;
            }

            _logger.LogInformation($"Processing OrderPlacedEvent for Order {orderEvent.OrderId}");

            // Step 1: Validate the order
            var validationResult = _orderValidator.Validate(orderEvent);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning($"Order validation failed for {orderEvent.OrderId}: {validationResult.Message}");
                // In a real system, you might publish an OrderRejectedEvent here
                return;
            }

            _logger.LogInformation($"Order {orderEvent.OrderId} validation passed");

            // Step 2: Execute the order
            var executionResult = await _orderExecutor.ExecuteOrderAsync(orderEvent);
            if (!executionResult.IsSuccessful)
            {
                _logger.LogError($"Order execution failed for {orderEvent.OrderId}: {executionResult.ErrorMessage}");
                // In a real system, you might publish an OrderFailedEvent here
                return;
            }

            _logger.LogInformation($"Order {orderEvent.OrderId} executed successfully with ExecutionId: {executionResult.ExecutionId}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming OrderPlacedEvent");
        }
    }
    
}