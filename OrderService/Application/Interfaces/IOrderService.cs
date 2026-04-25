using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces;

public interface IOrderService
{
    Task<List<Order>> GetOrderAsync(string status);
}