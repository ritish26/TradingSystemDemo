using System.Text.Json;
using Shared.Events;
using Shared.Infrastructure.Helper;
using Shared.Infrastructure.RabbitMqConnection;

namespace OrderService.Service;

public class OrderPublisher
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly ILogger<OrderPublisher> _logger;
    private const string OrderPlacedEventQueueName = "order-placed-events";

    public OrderPublisher(RabbitMqConnection rabbitMqConnection, ILogger<OrderPublisher> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _logger = logger;
    }

    public Task PublishOrderPlacedEventAsync(OrderPlacedEvent orderEvent)
    {
        try
        {
            var channel = _rabbitMqConnection.CreateChannel();

            channel.QueueDeclare(
                queue: OrderPlacedEventQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var message = JsonSerializer.Serialize(orderEvent);
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            // Add correlation ID for distributed tracing
            properties.SetCorrelationId();

            channel.BasicPublish(
                exchange: "",
                routingKey: OrderPlacedEventQueueName,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation($"OrderPlacedEvent published for Order {orderEvent.OrderId}");
            channel.Close();
            channel.Dispose();
        }
        
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderPlacedEvent");
            throw;
        }
        return Task.CompletedTask;
    }
}

