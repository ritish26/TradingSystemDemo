using System.Text.Json;
using RabbitMQ.Client;
using OrderService2.Event;

namespace OrderService2.Service;

public class OrderPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<OrderPublisher> _logger;
    private const string OrderPlacedEventQueueName = "order-placed-events";

    public OrderPublisher(IConfiguration configuration, ILogger<OrderPublisher> logger)
    {
        _logger = logger;
        
        var hostname = configuration["RabbitMq:HostName"] ?? "localhost";
        var port = int.Parse(configuration["RabbitMq:Port"] ?? "5672");
        var username = configuration["RabbitMq:UserName"] ?? "guest";
        var password = configuration["RabbitMq:Password"] ?? "guest";

        var factory = new ConnectionFactory()
        {
            HostName = hostname,
            Port = port,
            UserName = username,
            Password = password,
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnection();
    }

    public Task PublishOrderPlacedEventAsync(OrderPlacedEvent orderEvent)
    {
        return Task.Run(() =>
        {
            try
            {
                var channel = _connection.CreateModel();
                
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
        });
    }
}

