using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.API.Controllers.Contracts;
using OrderService.Application.Command;
using OrderService.Application.Queries;
using Shared.Domain.Constants;
using Shared.Domain.Exceptions;

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
        var command = _mapper.Map<OrderCreatedCommand>(orderRequest);
        command.OrderId = "ORDER-" + Guid.NewGuid();

        try
        {
            var orderId = await _mediator.Send(command);
            var response = new OrderResponse(orderId, Constants.Pending, "Order command published for processing");
            return Accepted(response);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogError(ex, "Authorization failed");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Authentication failed");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating order");
            throw;
        }
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

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Order Services is healthy" });
    }
}