using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ProcessingService.Infrastructure;
using ProcessingService.Consumers;

namespace ProcessingService.BackgroundService;

/// <summary>
/// RabbitConsumerService - Background service that listens to RabbitMQ for OrderPlaced events
/// Implements IHostedService for dependency injection in Program.cs
/// </summary>
public class RabbitConsumerService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly OrderPlacedConsumer _orderPlacedConsumer;
    private readonly ILogger<RabbitConsumerService> _logger;
    private IModel _channel;
    private const string OrderPlacedEventQueueName = "order-placed-events";

    public RabbitConsumerService(
        RabbitMqConnection rabbitMqConnection,
        OrderPlacedConsumer orderPlacedConsumer,
        ILogger<RabbitConsumerService> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _orderPlacedConsumer = orderPlacedConsumer;
        _logger = logger;
    }

    /// <summary>
    /// StartAsync - Called when the service starts
    /// </summary>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitConsumerService is starting");
        return base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// ExecuteAsync - Main execution loop for the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Create channel from connection
            _channel = _rabbitMqConnection.CreateChannel();

            // Declare queue (idempotent)
            _channel.QueueDeclare(
                queue: OrderPlacedEventQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _logger.LogInformation($"RabbitMQ queue '{OrderPlacedEventQueueName}' declared and listening");

            // Set Quality of Service (QoS) - prefetch count of 1 message at a time
            _channel.BasicQos(0, 1, false);
            _logger.LogInformation("QoS set: prefetching 1 message at a time");

            // Create consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // Set up event handler for receiving messages
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = System.Text.Encoding.UTF8.GetString(body);

                    _logger.LogInformation($"Message received from queue: {message}");

                    // Process the message
                    await _orderPlacedConsumer.ConsumeAsync(message);

                    // Acknowledge the message (remove from queue) after successful processing
                    _channel.BasicAck(ea.DeliveryTag, false);

                    _logger.LogInformation("Message processed and acknowledged");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message. Rejecting message.");
                    // Reject the message and requeue it
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            // Start consuming messages with manual acknowledgement
            _channel.BasicConsume(
                queue: OrderPlacedEventQueueName,
                autoAck: false, // FIXED: Changed from true to false for manual acknowledgement
                consumerTag: "order-placed-consumer",
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer
            );

            _logger.LogInformation("RabbitConsumerService started and waiting for messages");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitConsumerService is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in RabbitConsumerService");
            throw;
        }
    }

    /// <summary>
    /// StopAsync - Called when the service stops
    /// </summary>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitConsumerService is stopping");
        
        _channel?.Close();
        _channel?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}