using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces;

public interface IOrderRepository
{
    Task CreateOrderWithOutboxAsync(Order order, OutboxMessage message);
    Task<List<Order>> GetByStatusAsync(string status);

}