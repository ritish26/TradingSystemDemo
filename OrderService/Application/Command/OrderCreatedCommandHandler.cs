using MediatR;
using OrderService.Application.Factories;
using OrderService.Application.Interfaces;

namespace OrderService.Application.Command;

public class OrderCreatedCommandHandler : IRequestHandler<OrderCreatedCommand, Unit>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderFactory _orderFactory;

    public OrderCreatedCommandHandler(
        IOrderRepository orderRepository,
        ILogger<OrderCreatedCommandHandler> logger,
        IOrderFactory orderFactory)
    {
        _orderRepository = orderRepository;
        _orderFactory = orderFactory;
    }

    public async Task<Unit> Handle(OrderCreatedCommand request, CancellationToken cancellationToken)
    {
        var order = _orderFactory.CreateOrder(request);
        var outbox = _orderFactory.CreateOutboxMessage(request);

        await _orderRepository.CreateOrderWithOutboxAsync(order, outbox);

        return Unit.Value;
    }
}