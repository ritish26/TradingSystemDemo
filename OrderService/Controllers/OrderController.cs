using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FluentValidation;
using OrderService2.Model;
using OrderService2.Command;
using OrderService2.Mediator;
using Serilog.Context;
using Shared.Infrastructure;

namespace OrderService2.Controller;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly ICommandMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IValidator<OrderRequest> _orderRequestValidator;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        ICommandMediator mediator,
        IMapper mapper,
        IValidator<OrderRequest> orderRequestValidator,
        ILogger<OrderController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _orderRequestValidator = orderRequestValidator;
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
        // and available in all logs via Serilog LogContext
        
        try
        {
            _logger.LogInformation("CreateOrder request initiated for symbol {Symbol}", orderRequest.OrderType);

            // Validate request using Fluent Validation
            var validationResult = await _orderRequestValidator.ValidateAsync(orderRequest);
            
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Order validation failed: {ValidationErrors}", 
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(new 
                { 
                    errors = validationResult.Errors.Select(e => new 
                    { 
                        field = e.PropertyName, 
                        message = e.ErrorMessage 
                    })
                });
            }

            // Map OrderRequest to OrderCreatedCommand using AutoMapper
            var command = _mapper.Map<OrderCreatedCommand>(orderRequest);
            
            // Send command through mediator to handler
            await _mediator.SendAsync(command);

            _logger.LogInformation("Order command processed successfully");

            return Accepted(new 
            { 
                orderId = command.OrderId, 
                status = "PENDING", 
                message = "Order command published for processing" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new 
            { 
                error = "Internal server error", 
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        _logger.LogInformation("Order Service health check performed");
        return Ok(new { status = "Order Service is healthy" });
    }
}