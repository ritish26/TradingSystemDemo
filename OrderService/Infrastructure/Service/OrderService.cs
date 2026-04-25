using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo)
    {
        _repo = repo;
    }
    public async Task<List<Order>> GetOrderAsync(string status)
    {
        return await _repo.GetByStatusAsync(status);
    }
}