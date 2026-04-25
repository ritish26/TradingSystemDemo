using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
namespace OrderService.Infrastructure.Repositories;

/// <summary>
/// Domain    → knows NOTHING outside itself
/// Application → knows Domain only
/// Infrastructure → implements Application + Domain interfaces
/// API        → wires everything together (DI Registration)
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateOrderWithOutboxAsync(Order order, OutboxMessage message)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.Orders.AddAsync(order);
            await _context.OutboxMessages.AddAsync(message);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<List<Order>> GetByStatusAsync(string status)
    {
        return await _context.Orders
            .Where(o => o.Status == status)
            .ToListAsync();
    }
    
}