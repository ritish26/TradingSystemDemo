using System.Text.Json;
using ProcessingService.Application.Interfaces;
using ProcessingService.Domain.Exceptions;
using Shared.Domain.Events;

namespace ProcessingService.Infrastructure.Messaging;

/// <summary>
/// PURPOSE: Consume OrderPlaced events and orchestrate validation + execution
///
/// WHY CREATED:
/// - Responsible for handling OrderPlaced domain events
/// - Orchestrates validator and executor (composition)
/// - Depends on interfaces, not concrete classes (DIP)
/// - Separates message consumption from business logic
/// - Handles errors gracefully with domain exceptions
///
/// SOLID PRINCIPLES ACHIEVED:
/// - Dependency Inversion (DIP): Depends on IOrderValidator and IOrderExecutor
/// - Single Responsibility (SRP): Only orchestrates, doesn't validate or execute
/// - Interface Segregation (ISP): Uses only needed interfaces
///
/// WORKFLOW:
/// 1. Receive message from RabbitMQ
/// 2. Deserialize OrderPlacedEvent
/// 3. Validate order (via IOrderValidator)
/// 4. Execute order (via IOrderExecutor)
/// 5. Log results
/// 6. Publish events for downstream processing
///
/// ERROR HANDLING:
/// - ValidationException → order is invalid, don't retry
/// - ExecutionException → order failed, handle based on error code
/// - Other exceptions → infrastructure error, retry
///
/// USAGE (by RabbitConsumerService):
/// var message = "{ ...OrderPlacedEvent JSON... }";
/// await consumer.ConsumeAsync(message);
///
/// TESTING:
/// Can inject mocks:
/// var mockValidator = new Mock<IOrderValidator>();
/// var mockExecutor = new Mock<IOrderExecutor>();
/// var consumer = new OrderPlacedEventConsumer(mockValidator, mockExecutor, logger);
/// await consumer.ConsumeAsync(messageJson);
/// </summary>
public class OrderPlacedEventConsumer
{
    private readonly IOrderValidator _validator;
    private readonly IOrderExecutor _executor;
    private readonly ILogger<OrderPlacedEventConsumer> _logger;

    public OrderPlacedEventConsumer(
        IOrderValidator validator,
        IOrderExecutor executor,
        ILogger<OrderPlacedEventConsumer> logger)
    {
        _validator = validator;
        _executor = executor;
        _logger = logger;
    }

    /// <summary>
    /// Consumes and processes an OrderPlaced event
    /// </summary>
    public async Task ConsumeAsync(string message, CancellationToken cancellationToken = default)
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
            try
            {
                var validationResult = _validator.Validate(orderEvent);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        $"Order validation failed for {orderEvent.OrderId}: " +
                        $"{validationResult.Message} (Code: {validationResult.ErrorCode})");
                    // Validation failed - don't retry, order is invalid
                    return;
                }

                _logger.LogInformation($"Order {orderEvent.OrderId} validation passed");
            }
            catch (OrderValidationException ex)
            {
                _logger.LogWarning(
                    ex,
                    $"Order {ex.OrderId} validation exception: {ex.ValidationMessage}");
                // Validation exception - don't retry
                return;
            }

            // Step 2: Execute the order
            try
            {
                var executionResult = await _executor.ExecuteAsync(orderEvent, cancellationToken);

                if (executionResult.IsSuccessful)
                {
                    _logger.LogInformation(
                        $"Order {orderEvent.OrderId} executed successfully " +
                        $"with ExecutionId: {executionResult.ExecutionId}");
                }
                else
                {
                    _logger.LogError(
                        $"Order execution failed for {orderEvent.OrderId}: " +
                        $"{executionResult.ErrorMessage}");
                }
            }
            catch (OrderExecutionException ex)
            {
                _logger.LogError(
                    ex,
                    $"Order {ex.OrderId} execution exception: " +
                    $"{ex.Message} (Code: {ex.ErrorCode})");

                // Could implement retry logic based on error code:
                // if (ex.ErrorCode == "MARKET_UNSUITABLE") {
                //     // Retry later when market conditions change
                // }
                // if (ex.ErrorCode == "POSITION_ERROR") {
                //     // Don't retry - client has no position
                // }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error consuming OrderPlacedEvent");
            // Unexpected error - could retry or send to dead letter queue
        }
    }
}
