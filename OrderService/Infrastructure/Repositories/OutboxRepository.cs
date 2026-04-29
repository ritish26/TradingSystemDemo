using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

/// <summary>
/// Repository for outbox message persistence.
/// ISP-compliant: only exposes operations needed by OutboxProcessor, not the full DbContext.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;

    public OutboxRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OutboxMessage>> GetPendingAsync()
    {
        return await _context.OutboxMessages
            .Where(x => x.Status == OrderStatus.Pending)
            .ToListAsync();
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}
