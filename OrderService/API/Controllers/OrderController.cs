using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.API.Controllers.Contracts;
using OrderService.Application.Command;
using OrderService.Application.Queries;
using Shared.Domain.Constants;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IMediator mediator,
        IMapper mapper,
        ILogger<OrderController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Create a new trade order
    /// </summary>
    /// <param name="orderRequest">Order details</param>
    /// <returns>Order ID</returns>
    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
    {
        // Correlation ID is automatically set by the CorrelationIdMiddleware
        // And available in all logs via Serilog LogContext
        _logger.LogInformation("CreateOrder request initiated for symbol {Symbol}", orderRequest.OrderType);

        // Map OrderRequest to OrderCreatedCommand using AutoMapper
        var command = _mapper.Map<OrderCreatedCommand>(orderRequest);

        // Send command through mediator to handler
        var orderId = await _mediator.Send(command);

        _logger.LogInformation("Order command processed successfully");

        var response = new OrderResponse(orderId, Constants.Pending, "Order command published for processing");
        return Accepted(response);
    }
    
    
    /// <summary>
    /// Get order details Status can be PENDING, EXECUTED, REJECTED
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status)
    {
        var query = new GetOrdersQuery
        {
            Status = status
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        _logger.LogInformation("Order Services health check performed");
        return Ok(new { status = "Order Services is healthy" });
    }
}