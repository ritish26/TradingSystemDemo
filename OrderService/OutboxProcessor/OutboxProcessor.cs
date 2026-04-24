using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Data;
using OrderService.Service;
using Shared.Events;

namespace OrderService.OutboxProcessor;

public class OutboxProcessor
{
    private readonly AppDbContext _context;
    private readonly OrderPublisher _publisher;

    public OutboxProcessor(AppDbContext context, OrderPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task ProcessAsync()
    {
        var messages = await _context.OutboxMessages
            .Where(x => x.Status == "Pending")
            .ToListAsync();

        foreach (var msg in messages)
        {
            try
            {
                var eventObj = JsonSerializer.Deserialize<OrderPlacedEvent>(msg.Payload);

                await _publisher.PublishOrderPlacedEventAsync(eventObj);

                msg.Status = "Processed";
                msg.ProcessedAt = DateTime.UtcNow;
            }
            catch
            {
                msg.RetryCount++;
                msg.Status = msg.RetryCount >= 3 ? "Failed" : "Pending";
            }
        }

        await _context.SaveChangesAsync();
    }
}