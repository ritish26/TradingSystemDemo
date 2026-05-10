using AutoMapper;
using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.Application.Queries;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;

    public GetOrdersQueryHandler(IOrderService orderService, IMapper mapper)
    {
        _orderService = orderService;
        _mapper = mapper;
    }

    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Status, nameof(request.Status));

        var orders = await _orderService.GetOrderAsync(request.Status!);
        return _mapper.Map<List<OrderDto>>(orders);
    }
}