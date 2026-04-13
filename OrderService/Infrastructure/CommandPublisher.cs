using System.Text.Json;
using OrderService2.Command;

namespace OrderService2.Messaging;

public class CommandPublisher
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly ILogger<CommandPublisher> _logger;
    private const string OrderCommandQueueName = "order-created-commands";

    public CommandPublisher(RabbitMqConnection rabbitMqConnection, ILogger<CommandPublisher> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _logger = logger;
    }

    public Task PublishCommandAsync(OrderCreatedCommand command)
    {
        return Task.Run(() =>
        {
            try
            {
                var channel = _rabbitMqConnection.CreateChannel();
                
                // Declare the queue (idempotent operation)
                channel.QueueDeclare(
                    queue: OrderCommandQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var message = JsonSerializer.Serialize(command);
                var body = System.Text.Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";

                channel.BasicPublish(
                    exchange: "",
                    routingKey: OrderCommandQueueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation($"Command published for Order {command.OrderId}");
                channel.Close();
                channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing command");
                throw;
            }
        });
    }
}

