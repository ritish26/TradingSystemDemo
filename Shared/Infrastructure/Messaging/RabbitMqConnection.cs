using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Messaging;

// Implements IMessagingConnection for RabbitMQ. Encapsulates RabbitMQ-specific connection logic.
// Follows DIP - consumers depend on abstraction, allowing easy swap to other messaging systems.
public class RabbitMqConnection : IMessagingConnection, IDisposable
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqConnection> _logger;

    public RabbitMqConnection(IConfigurationProvider configProvider, ILogger<RabbitMqConnection> logger)
    {
        _logger = logger;

        var hostname = configProvider.GetValue("RabbitMq:HostName", "localhost");
        var port = configProvider.GetValue("RabbitMq:Port", 5672);
        var username = configProvider.GetValue("RabbitMq:UserName", "guest");
        var password = configProvider.GetValue("RabbitMq:Password", "guest");

        var factory = new ConnectionFactory()
        {
            HostName = hostname,
            Port = port,
            UserName = username,
            Password = password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(30),
            RequestedChannelMax = 0,
            DispatchConsumersAsync = true,
            ConsumerDispatchConcurrency = 1
        };

        try
        {
            _connection = factory.CreateConnection();
            _logger.LogInformation("RabbitMQ connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connecting to RabbitMQ at: {HostName}:{Port}", hostname, port);
            throw;
        }
    }

    public IModel CreateChannel()
    {
        return _connection.CreateModel();
    }

    public void Close()
    {
        _connection?.Close();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}