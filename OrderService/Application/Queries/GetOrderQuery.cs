using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Queries;

public class GetOrdersQuery : IRequest<List<OrderDto>>
{
    public required string Status { get; set; }
}