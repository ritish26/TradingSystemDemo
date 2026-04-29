using AutoMapper;
using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.Application.Queries;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly ILogger<GetOrdersQueryHandler> _logger;
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;
    
    public GetOrdersQueryHandler(ILogger<GetOrdersQueryHandler> logger, IOrderService orderService, IMapper mapper)
    {
        _logger = logger;
        _orderService = orderService;
        _mapper = mapper;
    }
    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
         ArgumentNullException.ThrowIfNull(request.Status, nameof(request.Status));
        _logger.LogInformation($"Handling GetOrdersQuery with status filter: {request.Status}");
        
        var logContext = new Dictionary<string, object>
        {
            { "StatusFilter", request.Status ?? "None" }
        };
        
        using var logScope = _logger.BeginScope(logContext);

        var orders = await _orderService.GetOrderAsync(request.Status!);
        var result = _mapper.Map<List<OrderDto>>(orders);
        
        return result ;
    }
}