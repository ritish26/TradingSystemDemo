using OrderService.Application.Factories;
using OrderService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderService.Application.Command;

public class OrderCreatedCommandHandler : IRequestHandler<OrderCreatedCommand, string>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderFactory _orderFactory;
    private readonly ILogger<OrderCreatedCommandHandler> _logger;

    public OrderCreatedCommandHandler(
        IOrderRepository orderRepository,
        ILogger<OrderCreatedCommandHandler> logger,
        IOrderFactory orderFactory)
    {
        _orderRepository = orderRepository;
        _orderFactory = orderFactory;
        _logger = logger;
    }

    public async Task<string> Handle(OrderCreatedCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Handler] OrderCreatedCommandHandler.Handle() started");

        request.OrderId = "ORDER-" + Guid.NewGuid();
        _logger.LogInformation("[Handler] Generated OrderId: {OrderId}", request.OrderId);

        _logger.LogInformation("[Handler] Creating Order aggregate from command...");
        var order = _orderFactory.CreateOrder(request);

        _logger.LogInformation("[Handler] Creating OutboxMessage...");
        var outbox = _orderFactory.CreateOutboxMessage(request);

        _logger.LogInformation("[Handler] Saving Order and OutboxMessage to repository...");
        await _orderRepository.CreateOrderWithOutboxAsync(order, outbox);

        _logger.LogInformation("[Handler] ✅ Order created successfully. OrderId: {OrderId}", request.OrderId);

        return request.OrderId;
    }
}