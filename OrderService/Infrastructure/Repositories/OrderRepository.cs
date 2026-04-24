using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories;

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
}