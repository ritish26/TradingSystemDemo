using ProcessingService.Application.Interfaces;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Domain.ValueObjects;
using Shared.Domain.Enum;
using Shared.Domain.Events;

namespace ProcessingService.Infrastructure.Services;

/// <summary>
/// PURPOSE: Execute validated orders in the trading system
///
/// WHY CREATED:
/// - Implementation of IOrderExecutor interface
/// - Single responsibility: only executes orders
/// - Depends on interfaces, not concrete implementations (DIP)
/// - Clean orchestration of multiple concerns:
///   * Market data checks
///   * Client position validation
///   * Trade execution
///   * Event publishing
///
/// SOLID PRINCIPLES ACHIEVED:
/// - Single Responsibility (SRP): Only executes, doesn't validate or publish directly
/// - Dependency Inversion (DIP): Depends on 3 interfaces, not concrete classes
/// - Open-Closed (OCP): Can swap market/position providers without changing executor
/// - Interface Segregation (ISP): Uses focused interfaces
///
/// DEPENDENCY INJECTION BENEFITS:
/// - IMarketDataProvider: Get prices, check market conditions
/// - IClientPositionProvider: Check if client can sell
/// - IOrderExecutionEventPublisher: Publish execution results
/// - ILogger: Log execution details
///
/// EXECUTION FLOW:
/// 1. Check market conditions (via IMarketDataProvider)
/// 2. Check client position if SELL order (via IClientPositionProvider)
/// 3. Get current price (via IMarketDataProvider)
/// 4. Execute trade
/// 5. Publish event (via IOrderExecutionEventPublisher)
///
/// TESTING:
/// Easy to test - just inject mocks:
/// var mockMarket = new Mock<IMarketDataProvider>();
/// var mockPositions = new Mock<IClientPositionProvider>();
/// var mockPublisher = new Mock<IOrderExecutionEventPublisher>();
/// var executor = new DefaultOrderExecutor(mockMarket, mockPositions, mockPublisher, logger);
///
/// EXTENSIBILITY:
/// Can add new behavior without modifying executor:
/// - Change market data source
/// - Add different position checking logic
/// - Add different event publishers
/// - Inject decorators for retry, caching, etc.
/// </summary>
public class DefaultOrderExecutor : IOrderExecutor
{
    private readonly IMarketDataProvider _marketData;
    private readonly IClientPositionProvider _positions;
    private readonly IOrderExecutionEventPublisher _eventPublisher;
    private readonly ILogger<DefaultOrderExecutor> _logger;

    public DefaultOrderExecutor(
        IMarketDataProvider marketData,
        IClientPositionProvider positions,
        IOrderExecutionEventPublisher eventPublisher,
        ILogger<DefaultOrderExecutor> logger)
    {
        _marketData = marketData;
        _positions = positions;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<OrderExecutionResult> ExecuteAsync(
        OrderPlacedEvent order,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Executing order {order.OrderId} for instrument {order.InstrumentSymbol}");

            // Step 1: Check market conditions
            var marketStatus = await _marketData.GetMarketStatusAsync(
                order.InstrumentSymbol,
                cancellationToken);

            if (!marketStatus.IsHealthy)
            {
                var message = $"Market conditions not suitable: {marketStatus.Reason}";
                _logger.LogWarning(message);
                return OrderExecutionResult.Failure(message);
            }

            // Step 2: Check client position if SELL order
            if (order.OrderType == OrderType.SELL)
            {
                var hasPosition = await _positions.HasSufficientPositionAsync(
                    order.ClientId!,
                    order.InstrumentSymbol!,
                    order.Quantity,
                    cancellationToken);

                if (!hasPosition)
                {
                    var message = "Client does not have sufficient position to sell";
                    _logger.LogWarning($"{message} - {order.OrderId}");
                    return OrderExecutionResult.Failure(message);
                }
            }

            // Step 3: Get current market price
            var currentPrice = await _marketData.GetCurrentPriceAsync(
                order.InstrumentSymbol!,
                order.Price,
                cancellationToken);

            // Step 4: Simulate execution latency
            await Task.Delay(ProcessingConstants.ExecutionLatencyMs, cancellationToken);

            // Step 5: Execute trade
            var executionId = GenerateExecutionId();
            var totalCost = (int)order.Quantity * currentPrice;

            _logger.LogInformation(
                $"Trade executed: OrderId={order.OrderId}, ExecutionId={executionId}, " +
                $"Quantity={order.Quantity}, Price={currentPrice:C}, Total={totalCost:C}");

            // Step 6: Publish execution event
            await _eventPublisher.PublishOrderExecutedAsync(
                order.OrderId!,
                executionId,
                order,
                cancellationToken);

            return OrderExecutionResult.Success(
                executionId,
                $"Order {order.OrderId} executed successfully",
                new()
                {
                    { "ExecutedAt", DateTime.UtcNow },
                    { "ExecutionPrice", currentPrice },
                    { "TotalValue", totalCost }
                });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning($"Order execution cancelled for {order.OrderId}");
            return OrderExecutionResult.Failure("Order execution was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing order {order.OrderId}");
            return OrderExecutionResult.Failure($"Execution error: {ex.Message}");
        }
    }

    public async Task<OrderExecutionResult> ExecuteOrThrowAsync(
        OrderPlacedEvent order,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(order, cancellationToken);
        if (!result.IsSuccessful)
        {
            throw new OrderExecutionException(
                result.ErrorMessage!,
                order.OrderId,
                "EXECUTION_FAILED");
        }
        return result;
    }

    private static string GenerateExecutionId() =>
        $"EXEC-{Guid.NewGuid():N}".Substring(0, 20);
}
