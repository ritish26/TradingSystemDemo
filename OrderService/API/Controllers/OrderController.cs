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
        // Correlation ID is automatically set by the CorrelationIdMiddleware
        // And available in all logs via Serilog LogContext
        _logger.LogInformation("[Controller] CreateOrder request started");
        _logger.LogInformation("[Controller] OrderType: {OrderType}, ClientId: {ClientId}, InstrumentSymbol: {Symbol}",
            orderRequest.OrderType, orderRequest.ClientId, orderRequest.InstrumentSymbol);

        // Map OrderRequest to OrderCreatedCommand using AutoMapper
        var command = _mapper.Map<OrderCreatedCommand>(orderRequest);
        _logger.LogInformation("[Controller] Command mapped successfully");

        // Send command through mediator to handler
        try
        {
            _logger.LogInformation("[Controller] Sending command to MediatR pipeline...");
            var orderId = await _mediator.Send(command);

            _logger.LogInformation("[Controller] ✅ Order command processed successfully. OrderId: {OrderId}", orderId);

            var response = new OrderResponse(orderId, Constants.Pending, "Order command published for processing");
            _logger.LogInformation("[Controller] Returning 202 Accepted with OrderId: {OrderId}", orderId);
            return Accepted(response);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogError("[Controller] ❌ Authorization failed: {Error}", ex.Message);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("[Controller] ❌ Authentication failed: {Error}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("[Controller] ❌ Unexpected error: {Error}", ex.Message);
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