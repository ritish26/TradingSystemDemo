using OrderService2.Service;
using Shared.Events;

namespace OrderService2.Command;

public class OrderCreatedCommandHandler
{
    private readonly OrderPublisher _orderPublisher;
    private readonly ILogger<OrderCreatedCommandHandler> _logger;

    public OrderCreatedCommandHandler(OrderPublisher orderPublisher, ILogger<OrderCreatedCommandHandler> logger)
    {
        _orderPublisher = orderPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedCommand command)
    {
        try
        {
            _logger.LogInformation($"Processing command for Order {command.OrderId}");
            
            var logContext = new Dictionary<string, object>
            {
                { "RequestId", "Command Handler Execution Starts" },
            };

            using var logScope = _logger.BeginScope(logContext);

            // Validate command
            if (string.IsNullOrEmpty(command.ClientId) || string.IsNullOrEmpty(command.InstrumentSymbol))
            {
                throw new ArgumentException("ClientId and InstrumentSymbol are required");
            }

            // Create OrderPlacedEvent from command
            var orderEvent = new OrderPlacedEvent
            {
                OrderId = command.OrderId,
                ClientId = command.ClientId,
                InstrumentSymbol = command.InstrumentSymbol,
                OrderType = command.OrderType,
                Quantity = command.Quantity,
                Price = command.Price,
                Status = "PLACED",
                CreatedAt = command.CreatedAt
            };
            
            await _orderPublisher.PublishOrderPlacedEventAsync(orderEvent);

            _logger.LogInformation($"Command handled successfully for Order {command.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling command for Order {command.OrderId}");
            throw;
        }
    }
}

