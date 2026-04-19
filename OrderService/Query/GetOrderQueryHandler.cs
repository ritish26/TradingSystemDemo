using MediatR;
using OrderService.DataAcessLayer;

namespace OrderService.Query;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    public Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = OrderStore.GetAll().FindAll(o => o.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(orders);
    }
}