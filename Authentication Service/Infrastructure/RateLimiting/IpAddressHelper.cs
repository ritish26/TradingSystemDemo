using Microsoft.AspNetCore.Http;

namespace Authentication_Service.Infrastructure.RateLimiting;

/// <summary>
/// Helper for safely extracting real client IP.
///
/// Problem: Behind load balancer, RemoteIpAddress always shows load balancer IP.
/// Solution: Read X-Forwarded-For, but ONLY from known proxies to prevent spoofing.
///
/// Attack Example:
/// - Attacker sends: X-Forwarded-For: 1.2.3.4 (fake IP)
/// - If we trust it blindly, attacker bypasses per-IP rate limiting
///
/// Defense: Only trust X-Forwarded-For from known load balancer IPs.
/// </summary>
public static class IpAddressHelper
{
    /// <summary>
    /// Extract real client IP, accounting for load balancers.
    /// </summary>
    public static string GetClientIp(HttpContext context, IConfiguration configuration)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(remoteIp))
            return "unknown";

        // Only trust X-Forwarded-For from known proxies (load balancers)
        var knownProxies = configuration
            .GetSection("RateLimiting:KnownProxies")
            .Get<string[]>() ?? Array.Empty<string>();

        // If direct connection is from a known proxy, trust X-Forwarded-For
        if (knownProxies.Contains(remoteIp))
        {
            var forwarded = context.Request
                .Headers["X-Forwarded-For"]
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(forwarded))
            {
                // X-Forwarded-For can be comma-separated list (client, proxy1, proxy2)
                // Get the first one (actual client IP)
                var clientIp = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(clientIp))
                    return clientIp;
            }
        }

        // Either not from known proxy, or no X-Forwarded-For header
        // Use RemoteIpAddress (load balancer IP or direct client)
        return remoteIp;
    }
}
