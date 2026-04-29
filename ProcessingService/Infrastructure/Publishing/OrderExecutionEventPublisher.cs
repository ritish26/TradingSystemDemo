using System.Text.Json;
using ProcessingService.Application.Interfaces;
using ProcessingService.Domain.ValueObjects;
using Shared.Domain.Events;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Messaging;

namespace ProcessingService.Infrastructure.Publishing;

/// <summary>
/// PURPOSE: Publish order execution events to message queue
///
/// WHY CREATED:
/// - Implementation of IOrderExecutionEventPublisher
/// - Publishes execution results for downstream systems
/// - Enables asynchronous communication (Messaging Pattern)
/// - Decouples executor from specific messaging technology
/// - Observable Pattern: interested systems can subscribe
///
/// SOLID PRINCIPLES:
/// - Single Responsibility: Only publishes, doesn't execute
/// - Dependency Inversion: Implements interface
/// - Open-Closed: Can be replaced with Kafka/Azure Bus implementation
///
/// OBSERVER PATTERN:
/// When order executes, this publishes events:
/// - OrderExecutedEvent → other services react (notifications, reporting, etc.)
/// - OrderExecutionFailedEvent → alert operations, retry logic
///
/// USAGE IN EXECUTOR:
/// await publisher.PublishOrderExecutedAsync(orderId, executionId, order);
///
/// SUBSCRIBERS:
/// - Notification Service: Send alerts to client
/// - Reporting Service: Record execution in data warehouse
/// - Risk Service: Update risk metrics
/// - Compliance Service: Audit trail
/// - Settlement Service: Process settlement
///
/// FUTURE ENHANCEMENTS:
/// - Add retry logic with exponential backoff
/// - Add dead letter queue for failed publishes
/// - Add publishing timeout
/// - Add batch publishing for throughput
/// </summary>
public class OrderExecutionEventPublisher : IOrderExecutionEventPublisher
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<OrderExecutionEventPublisher> _logger;

    public OrderExecutionEventPublisher(
        RabbitMqConnection connection,
        ILogger<OrderExecutionEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishOrderExecutedAsync(
        string orderId,
        string executionId,
        OrderPlacedEvent orderEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Publishing OrderExecutedEvent for {orderId} (ExecutionId: {executionId})");

            var channel = _connection.CreateChannel();

            // Declare queue (idempotent - won't fail if exists)
            channel.QueueDeclare(
                queue: ProcessingConstants.OrderExecutedEventQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var @event = new { orderId, executionId, executedAt = DateTime.UtcNow };
            var message = JsonSerializer.Serialize(@event);
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.SetCorrelationId(); // Add correlation ID for tracing

            channel.BasicPublish(
                exchange: "",
                routingKey: ProcessingConstants.OrderExecutedEventQueueName,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation($"OrderExecutedEvent published for {orderId}");

            channel.Close();
            channel.Dispose();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing OrderExecutedEvent for {orderId}");
            throw;
        }
    }

    public async Task PublishOrderExecutionFailedAsync(
        string orderId,
        string reason,
        OrderPlacedEvent orderEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Publishing OrderExecutionFailedEvent for {orderId}: {reason}");

            var channel = _connection.CreateChannel();

            channel.QueueDeclare(
                queue: ProcessingConstants.OrderFailedEventQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var @event = new { orderId, reason, failedAt = DateTime.UtcNow };
            var message = JsonSerializer.Serialize(@event);
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.SetCorrelationId();

            channel.BasicPublish(
                exchange: "",
                routingKey: ProcessingConstants.OrderFailedEventQueueName,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation($"OrderExecutionFailedEvent published for {orderId}");

            channel.Close();
            channel.Dispose();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing OrderExecutionFailedEvent for {orderId}");
            throw;
        }
    }
}
