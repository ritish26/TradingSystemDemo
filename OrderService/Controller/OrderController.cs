using Microsoft.AspNetCore.Mvc;
using OrderService2.Model;
using OrderService2.Command;
using OrderService2.Messaging;

namespace OrderService2.Controller;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly CommandPublisher _commandPublisher;
    private readonly ILogger<OrderController> _logger;

    public OrderController(CommandPublisher commandPublisher, ILogger<OrderController> logger)
    {
        _commandPublisher = commandPublisher;
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
            // Validate request
            if (orderRequest == null || string.IsNullOrEmpty(orderRequest.ClientId))
            {
                return BadRequest("Invalid order request");
            }

            // Generate Order ID
            var orderId = Guid.NewGuid().ToString();

            // Create Command
            var command = new OrderCreatedCommand
            {
                OrderId = orderId,
                ClientId = orderRequest.ClientId,
                InstrumentSymbol = orderRequest.InstrumentSymbol,
                OrderType = orderRequest.OrderType,
                Quantity = orderRequest.Quantity,
                Price = orderRequest.Price,
                CreatedAt = DateTime.UtcNow
            };

            // Publish command to queue
            await _commandPublisher.PublishCommandAsync(command);

            _logger.LogInformation($"Order {orderId} command published successfully");

            return Accepted(new { orderId, status = "PENDING", message = "Order command published for processing" });
        }
        
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
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

