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
        var logContext = new Dictionary<string, object>
        {
            { "RequestId", Guid.NewGuid().ToString("N")[..8] },
            { "CommandName", nameof(GetStatus) },
            { "RequestType", "HealthCheck" },
            { "ServiceVersion", "1.0.0" }
        };

        using var logScope = _logger.BeginScope(logContext);
        _logger.LogInformation("Health check requested on Processing Service");
        
        return Ok(new
        {
            service = "ProcessingService",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            message = "Processing Service is running and listening for OrderPlaced events"
        });
    }
}

