using System.Text.Json;
using MediatR;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Repositories;
using Shared.Domain.Constants;

namespace OrderService.Application.Command;

public class OrderCreatedCommandHandler : IRequestHandler<OrderCreatedCommand, Unit>
{
    private readonly ILogger<OrderCreatedCommandHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private static readonly string OrderCreatedEvent = "OrderCreatedEvent";

    public OrderCreatedCommandHandler(IOrderRepository orderRepository, ILogger<OrderCreatedCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }
    public async Task<Unit> Handle(OrderCreatedCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            OrderId = request.OrderId,
            ClientId = request.ClientId,
            InstrumentId = request.InstrumentSymbol,
            OrderType = request.OrderType,
            Quantity = request.Quantity,
            Price = request.Price,
            CreatedAt = request.CreatedAt,
            Status = Constants.Pending
        };

        var outbox = new OutboxMessage
        {
            Id = CreateOutboxOrderid(),
            OrderId = request.OrderId,
            EventType = OrderCreatedEvent,
            Payload = JsonSerializer.Serialize(request),
            CreatedAt = DateTime.UtcNow,
            Status = Constants.Pending
        };

        await _orderRepository.CreateOrderWithOutboxAsync(order, outbox);

        return Unit.Value;
    }
    
    private Guid CreateOutboxOrderid()
    {
        return Guid.NewGuid();
    }
}