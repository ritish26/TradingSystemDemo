using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Authentication_Service.Application.Interfaces;

namespace Authentication_Service.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing IP whitelist.
/// Allows trusted admins to add/remove IPs without redeploying.
/// Examples: FNZ office, VPN gateways, partner systems.
/// </summary>
[ApiController]
[Route("api/whitelist")]
[Authorize]
public class WhitelistController : ControllerBase
{
    private readonly IWhitelistService _whitelistService;
    private readonly ILogger<WhitelistController> _logger;

    public WhitelistController(IWhitelistService whitelistService, ILogger<WhitelistController> logger)
    {
        _whitelistService = whitelistService;
        _logger = logger;
    }

    /// <summary>
    /// List all whitelisted IPs.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWhitelist()
    {
        var ips = await _whitelistService.GetAllWhitelistedIpsAsync();
        _logger.LogInformation("Whitelist retrieved: {Count} IPs", ips.Count());
        return Ok(new { whitelistedIps = ips });
    }

    /// <summary>
    /// Add IP to whitelist. Whitelisted IPs bypass rate limiting entirely.
    /// </summary>
    [HttpPost("{ipAddress}")]
    public async Task<IActionResult> AddToWhitelist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return BadRequest(new { error = "IP address cannot be empty" });

        // Basic IP validation (not comprehensive, good enough for admin use)
        if (!IsValidIpAddress(ipAddress))
            return BadRequest(new { error = "Invalid IP address format" });

        await _whitelistService.AddToWhitelistAsync(ipAddress);
        _logger.LogInformation("IP {IP} added to whitelist", ipAddress);

        return Ok(new { message = $"IP {ipAddress} added to whitelist" });
    }

    /// <summary>
    /// Remove IP from whitelist. Removed IPs will be rate limited.
    /// </summary>
    [HttpDelete("{ipAddress}")]
    public async Task<IActionResult> RemoveFromWhitelist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return BadRequest(new { error = "IP address cannot be empty" });

        await _whitelistService.RemoveFromWhitelistAsync(ipAddress);
        _logger.LogInformation("IP {IP} removed from whitelist", ipAddress);

        return Ok(new { message = $"IP {ipAddress} removed from whitelist" });
    }

    private static bool IsValidIpAddress(string ip)
    {
        // Accept IPv4 or IPv6
        return System.Net.IPAddress.TryParse(ip, out _);
    }
}
