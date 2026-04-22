using MediatR;
using OrderService.DataAcessLayer;

namespace OrderService.Query;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    public ILogger<GetOrdersQueryHandler> _logger;
    
    public GetOrdersQueryHandler(ILogger<GetOrdersQueryHandler> logger)
    {
        _logger = logger;
    }
    public Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Handling GetOrdersQuery with status filter: {request.Status}");
        
        var logContext = new Dictionary<string, object>
        {
            { "StatusFilter", request.Status ?? "None" }
        };
        using var logScope = _logger.BeginScope(logContext);
        
        var orders = OrderStore.GetAll().FindAll(o => o.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(orders);
    }
}