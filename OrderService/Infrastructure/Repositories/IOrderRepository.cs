using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Repositories;

public interface IOrderRepository
{
    Task CreateOrderWithOutboxAsync(Order order, OutboxMessage message);
}