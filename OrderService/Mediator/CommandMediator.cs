using OrderService2.Command;

namespace OrderService2.Mediator;

public class CommandMediator : ICommandMediator
{
    private readonly OrderCreatedCommandHandler _orderCreatedCommandHandler;
    private readonly ILogger<CommandMediator> _logger;

    public CommandMediator(OrderCreatedCommandHandler orderCreatedCommandHandler, ILogger<CommandMediator> logger)
    {
        _orderCreatedCommandHandler = orderCreatedCommandHandler;
        _logger = logger;
    }

    public async Task SendAsync<TCommand>(TCommand command) where TCommand : class
    {
        try
        {
            if (command is OrderCreatedCommand orderCommand)
            {
                _logger.LogInformation($"Mediator sending OrderCreatedCommand for Order {orderCommand.OrderId}");
                await _orderCreatedCommandHandler.HandleAsync(orderCommand);
            }
            else
            {
                throw new NotSupportedException($"Command type {typeof(TCommand).Name} is not supported");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing command of type {typeof(TCommand).Name}");
            throw;
        }
    }
}

