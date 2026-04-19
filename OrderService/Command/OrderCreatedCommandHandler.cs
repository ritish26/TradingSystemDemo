using MediatR;
using OrderService.Service;
using Shared.Events;

namespace OrderService.Command;

public class OrderCreatedCommandHandler : IRequestHandler<OrderCreatedCommand, Unit>
{
    private readonly OrderPublisher _orderPublisher;
    private readonly ILogger<OrderCreatedCommandHandler> _logger;

    public OrderCreatedCommandHandler(OrderPublisher orderPublisher, ILogger<OrderCreatedCommandHandler> logger)
    {
        _orderPublisher = orderPublisher;
        _logger = logger;
    }
    
    public async Task<Unit> Handle(OrderCreatedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Processing command for Order {request.OrderId}");
            
            var logContext = new Dictionary<string, object>
            {
                { "Test log in command Handler", "Command Handler Execution Starts" },
            };

            using var logScope = _logger.BeginScope(logContext);

            // Validate command
            if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.InstrumentSymbol))
            {
                throw new ArgumentException("ClientId and InstrumentSymbol are required");
            }

            // Create OrderPlacedEvent from command
            var orderEvent = new OrderPlacedEvent
            {
                OrderId = request.OrderId,
                ClientId = request.ClientId,
                InstrumentSymbol = request.InstrumentSymbol,
                OrderType = request.OrderType,
                Quantity = request.Quantity,
                Price = request.Price,
                Status = "PLACED",
                CreatedAt = request.CreatedAt
            };
            
             await _orderPublisher.PublishOrderPlacedEventAsync(orderEvent);

            _logger.LogInformation($"Command handled successfully for Order {request.OrderId}");
            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling command for Order {request.OrderId}");
            throw;
        }
    }
}

