using System.Text.Json;
using Shared.Domain.Constants;
using Shared.Domain.Events;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Messaging;

namespace OrderService.Infrastructure.Service;

public class OrderPublisher
{
    private readonly ILogger<OrderPublisher> _logger;
    
    private readonly RabbitMqConnection _rabbitMqConnection;
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
                queue: Constants.OrderPlacedEventQueueName,
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
                routingKey: Constants.OrderPlacedEventQueueName,
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

