using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Infrastructure.Messaging;

public class RabbitMqConnection
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqConnection> _logger;

    public RabbitMqConnection(IConfiguration configuration, ILogger<RabbitMqConnection> logger)
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
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(30), // Heartbeat every 30 seconds
            RequestedChannelMax = 0, // Unlimited channels
            DispatchConsumersAsync = true, // Important for async consumers
            ConsumerDispatchConcurrency = 1 // Process messages sequentially
        };

        try
        {
            _connection = factory.CreateConnection();
            _logger.LogInformation("RabbitMQ connection established");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, $"Connecting to RabbitMQ at: {factory.HostName}:{factory.Port}");
            throw;
        }
    }

    public IModel CreateChannel()
    {
        var channel = _connection.CreateModel();
        return channel;
    }
}