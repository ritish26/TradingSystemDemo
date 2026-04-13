using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService2.Command;
using OrderService2.Messaging;

namespace OrderService2.BackgroundServices;

public class CommandConsumerService : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly OrderCreatedCommandHandler _commandHandler;
    private readonly ILogger<CommandConsumerService> _logger;
    private IModel? _channel;
    private const string OrderCommandQueueName = "order-created-commands";

    public CommandConsumerService(
        RabbitMqConnection rabbitMqConnection,
        OrderCreatedCommandHandler commandHandler,
        ILogger<CommandConsumerService> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _commandHandler = commandHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CommandConsumerService is starting");

        try
        {
            _channel = _rabbitMqConnection.CreateChannel();

            // Declare the queue
            _channel.QueueDeclare(
                queue: OrderCommandQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // Set prefetch to 1 to ensure fair dispatch
            _channel.BasicQos(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var message = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation($"Received command: {message}");

                    var command = JsonSerializer.Deserialize<OrderCreatedCommand>(message);
                    if (command != null)
                    {
                        await _commandHandler.HandleAsync(command);
                        
                        // Acknowledge the message
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation($"Command acknowledged for Order {command.OrderId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing command");
                    // Nack the message to requeue it
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: OrderCommandQueueName,
                autoAck: false,
                consumerTag: "order-command-consumer",
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer
            );

            _logger.LogInformation("CommandConsumerService is listening for commands...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CommandConsumerService");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            _channel.Close();
            _channel.Dispose();
        }
        
        _logger.LogInformation("CommandConsumerService is stopping");
        await base.StopAsync(cancellationToken);
    }
}

