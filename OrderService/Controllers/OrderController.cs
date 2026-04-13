using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FluentValidation;
using OrderService2.Model;
using OrderService2.Command;
using OrderService2.Messaging;

namespace OrderService2.Controller;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly CommandPublisher _commandPublisher;
    private readonly IMapper _mapper;
    private readonly IValidator<OrderRequest> _orderRequestValidator;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        CommandPublisher commandPublisher,
        IMapper mapper,
        IValidator<OrderRequest> orderRequestValidator,
        ILogger<OrderController> logger)
    {
        _commandPublisher = commandPublisher;
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
        try
        {
            // Validate request using Fluent Validation
            var validationResult = await _orderRequestValidator.ValidateAsync(orderRequest);
            
            if (!validationResult.IsValid)
            {
                _logger.LogWarning($"Order validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
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

            // Publish command to queue
            await _commandPublisher.PublishCommandAsync(command);

            _logger.LogInformation($"Order {command.OrderId} command published successfully");

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
        return Ok(new { status = "Order Service is healthy" });
    }
}