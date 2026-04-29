using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces;

/// <summary>
/// ISP-compliant interface exposing only the outbox-specific persistence operations.
/// OutboxProcessor should depend on this, not on the full AppDbContext.
/// </summary>
public interface IOutboxRepository
{
    Task<List<OutboxMessage>> GetPendingAsync();
    Task SaveAsync();
}