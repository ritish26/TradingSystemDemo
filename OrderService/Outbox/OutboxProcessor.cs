using System.Text.Json;
using OrderService.Application.Converters;
using OrderService.Application.Interfaces;
using OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

namespace OrderService.Outbox;

/// <summary>
/// Processes pending outbox messages containing domain events and publishes integration events.
/// Translation layer: converts domain events (OrderCreated) to integration events (OrderPlacedEvent).
/// DDD: domain events stay internal, integration events cross service boundaries.
/// </summary>
public class OutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxRepository outboxRepository,
        IEventPublisher publisher,
        ILogger<OutboxProcessor> logger)
    {
        _outboxRepository = outboxRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessAsync()
    {
        var messages = await _outboxRepository.GetPendingAsync();

        foreach (var msg in messages)
        {
            try
            {
                if (msg.EventType == nameof(OrderCreated))
                {
                    var domainEvent = JsonSerializer.Deserialize<OrderCreated>(msg.Payload);
                    var integrationEvent = DomainEventToIntegrationEventConverter.Convert(domainEvent);
                    await _publisher.PublishOrderPlacedEventAsync(integrationEvent);
                }

                msg.Status = OrderStatus.Processed;
                msg.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", msg.Id);
                msg.RetryCount++;
                msg.Status = ShouldMarkAsFailed(msg.RetryCount) ? OrderStatus.Failed : OrderStatus.Pending;
            }
        }

        await _outboxRepository.SaveAsync();
    }

    private static bool ShouldMarkAsFailed(int retryCount) => retryCount >= 3;
}