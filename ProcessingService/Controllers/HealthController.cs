using Microsoft.AspNetCore.Mvc;

namespace ProcessingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        _logger.LogInformation("Health check requested");
        return Ok(new
        {
            service = "ProcessingService",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            message = "Processing Service is running and listening for OrderPlaced events"
        });
    }
}

