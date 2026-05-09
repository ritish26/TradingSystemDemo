using OrderService.Application.Factories;
using OrderService.Application.Interfaces;
using MediatR;

namespace OrderService.Application.Command;

public class OrderCreatedCommandHandler : IRequestHandler<OrderCreatedCommand, string>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderFactory _orderFactory;

    public OrderCreatedCommandHandler(
        IOrderRepository orderRepository,
        IOrderFactory orderFactory)
    {
        _orderRepository = orderRepository;
        _orderFactory = orderFactory;
    }

    public async Task<string> Handle(OrderCreatedCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.OrderId))
            throw new InvalidOperationException("OrderId must be set at the request boundary (controller).");

        var order = _orderFactory.CreateOrder(request);
        var outbox = _orderFactory.CreateOutboxMessage(request);

        await _orderRepository.CreateOrderWithOutboxAsync(order, outbox);

        return request.OrderId;
    }
}