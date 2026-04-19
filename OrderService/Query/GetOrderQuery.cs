using MediatR;
using OrderService.DataAcessLayer;

namespace OrderService.Query;

public class GetOrdersQuery : IRequest<List<OrderDto>>
{
    public string? Status { get; set; }
}