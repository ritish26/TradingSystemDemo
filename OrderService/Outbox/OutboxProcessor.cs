using System.Text.Json;
using OrderService.Application.Interfaces;
using OrderService.Domain.ValueObjects;
using Shared.Domain.Events;

namespace OrderService.Outbox;

/// <summary>
/// Processes pending outbox messages and publishes domain events.
/// Depends on abstractions (IEventPublisher, IOutboxRepository) for testability and flexibility.
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
                var eventObj = JsonSerializer.Deserialize<OrderPlacedEvent>(msg.Payload);

                await _publisher.PublishOrderPlacedEventAsync(eventObj);

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