using Shared.Enum;
using Shared.Events;

namespace ProcessingService.Service;

/// <summary>
/// OrderExecutor - Executes orders in the trading system
/// </summary>
public class OrderExecutor
{
    private readonly ILogger<OrderExecutor> _logger;

    public OrderExecutor(ILogger<OrderExecutor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes an order in the system
    /// </summary>
    public async Task<ExecutionResult> ExecuteOrderAsync(OrderPlacedEvent order)
    {
        try
        {
            _logger.LogInformation($"Executing order {order.OrderId} for instrument {order.InstrumentSymbol}");

            // Simulate trade execution latency
            await Task.Delay(100);

            // Execute the trade
            var executionResult = PerformTradeExecution(order);

            if (executionResult.IsSuccessful)
            {
                _logger.LogInformation($"Order {order.OrderId} executed successfully. ExecutionId: {executionResult.ExecutionId}");
            }
            else
            {
                _logger.LogWarning($"Order {order.OrderId} execution failed: {executionResult.ErrorMessage}");
            }

            return executionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Critical error executing order {order.OrderId}");
            return ExecutionResult.Failure($"Execution error: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs the actual trade execution logic
    /// </summary>
    private ExecutionResult PerformTradeExecution(OrderPlacedEvent order)
    {
        try
        {
            // Generate execution ID
            var executionId = $"EXEC-{Guid.NewGuid():N}".Substring(0, 20);

            // Business logic: Simulate market conditions check
            var marketCheck = CheckMarketConditions(order.InstrumentSymbol);
            if (!marketCheck.IsHealthy)
            {
                return ExecutionResult.Failure($"Market conditions not suitable: {marketCheck.Reason}");
            }

            // Business logic: Simulate availability check
            var availabilityCheck = CheckInventory(order);
            if (!availabilityCheck.IsAvailable)
            {
                return ExecutionResult.Failure($"Insufficient inventory: {availabilityCheck.Message}");
            }

            // Business logic: Execute trade with price at current market price
            var executionPrice = GetCurrentPrice(order.InstrumentSymbol, order.Price);
            var totalCost = order.Quantity * executionPrice;

            // Business logic: For SELL orders, ensure client has positions
            if (order.OrderType == OrderType.SELL)
            {
                var positionCheck = CheckClientPosition(order.ClientId, order.InstrumentSymbol, order.Quantity);
                if (!positionCheck)
                {
                    return ExecutionResult.Failure("Client does not have sufficient position to sell");
                }
            }

            // Log execution details
            _logger.LogInformation($"Trade executed: OrderId={order.OrderId}, ExecutionId={executionId}, Quantity={order.Quantity}, Price={executionPrice}, Total={totalCost}");

            return ExecutionResult.Success(executionId, $"Order {order.OrderId} executed at price {executionPrice}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in trade execution logic");
            return ExecutionResult.Failure($"Trade execution error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if market conditions are suitable for trading
    /// </summary>
    private MarketHealthCheck CheckMarketConditions(string? symbol)
    {
        // Simulate market health check
        // In production, this would query real market Data
        var isHealthy = !string.IsNullOrEmpty(symbol);
        return new MarketHealthCheck 
        { 
            IsHealthy = isHealthy,
            Reason = isHealthy ? "Market is open" : "Market is closed"
        };
    }

    /// <summary>
    /// Checks if the instrument is available for trading
    /// </summary>
    private InventoryCheck CheckInventory(OrderPlacedEvent order)
    {
        // Simulate inventory/availability check
        // In production, this would check actual inventory
        return new InventoryCheck
        {
            IsAvailable = true,
            Message = "Instrument is available"
        };
    }

    /// <summary>
    /// Gets the current market price for an instrument
    /// </summary>
    private decimal GetCurrentPrice(string symbol, decimal requestedPrice)
    {
        // Simulate getting current market price
        // In production, this would query real market Data
        // For now, use a slight variation from requested price
        var variance = (decimal)(new Random().NextDouble() * 0.02 - 0.01); // -1% to +1%
        return requestedPrice * (1 + variance);
    }

    /// <summary>
    /// Checks if the client has sufficient position to sell
    /// </summary>
    private bool CheckClientPosition(string clientId, string symbol, decimal quantity)
    {
        // Simulate client position check
        // In production, this would check actual portfolio
        return true; // Allow for demo purposes
    }
}

/// <summary>
/// Result object for trade execution
/// </summary>
public class ExecutionResult
{
    public bool IsSuccessful { get; set; }
    public string ExecutionId { get; set; }
    public string Message { get; set; }
    public string ErrorMessage { get; set; }

    public static ExecutionResult Success(string executionId, string message)
    {
        return new ExecutionResult
        {
            IsSuccessful = true,
            ExecutionId = executionId,
            Message = message,
            ErrorMessage = null
        };
    }

    public static ExecutionResult Failure(string errorMessage)
    {
        return new ExecutionResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            Message = null
        };
    }
}

/// <summary>
/// Market health check result
/// </summary>
public class MarketHealthCheck
{
    public bool IsHealthy { get; set; }
    public string Reason { get; set; }
}

/// <summary>
/// Inventory availability check result
/// </summary>
public class InventoryCheck
{
    public bool IsAvailable { get; set; }
    public string Message { get; set; }
}