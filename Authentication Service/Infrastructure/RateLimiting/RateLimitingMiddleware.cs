using Microsoft.AspNetCore.Http;
using Authentication_Service.Application.Interfaces;

namespace Authentication_Service.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiting middleware with IP whitelisting and dual-key protection.
///
/// Flow:
/// 1. Extract real client IP (handles load balancers)
/// 2. Check whitelist first — whitelisted IPs (FNZ office, VPN) bypass all limits
/// 3. If not whitelisted: dual-key rate limiting
///    - Per-IP limit: 20 per 300s (catches password spraying)
///    - Per-username limit: 5 per 300s (catches brute force)
/// 4. Both must pass, reject if either exceeded
///
/// Why dual-key?
/// - Per-IP stops attackers using same machine (office, VPN)
/// - Per-username stops attackers spreading across IPs (botnet)
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisRateLimiter _rateLimiter;
    private readonly IWhitelistService _whitelistService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        RedisRateLimiter rateLimiter,
        IWhitelistService whitelistService,
        IConfiguration configuration,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _whitelistService = whitelistService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsLoginEndpoint(context.Request.Path) && context.Request.Method == "POST")
        {
            // Step 1 — Get real client IP (handles load balancers)
            var ip = IpAddressHelper.GetClientIp(context, _configuration);

            // Step 2 — Check whitelist first (trusted IPs bypass all rate limiting)
            if (await _whitelistService.IsWhitelistedAsync(ip))
            {
                _logger.LogInformation("Whitelisted IP {IP} bypassing rate limit", ip);
                await _next(context);
                return;
            }

            try
            {
                // Step 3 — Enable body buffering so we can read it multiple times
                context.Request.EnableBuffering();

                // Step 4 — Extract and normalize username
                var username = await ExtractUsernameFromRequestAsync(context);
                username = username?.ToLowerInvariant(); // Prevent case-based bypass

                // Rewind for controller (only if stream supports seeking)
                if (context.Request.Body.CanSeek)
                    context.Request.Body.Position = 0;

                // Step 5 — Dual-key rate limiting
                var ipKey = $"auth-rate-limit:ip:{ip}";
                var userKey = username != null ? $"auth-rate-limit:user:{username}" : null;

                var ipMaxRequests = int.Parse(_configuration["RateLimiting:IpMaxRequests"] ?? "20");
                var windowSeconds = int.Parse(_configuration["RateLimiting:WindowSeconds"] ?? "300");

                var ipAllowed = await _rateLimiter.IsAllowedAsync(ipKey, ipMaxRequests, windowSeconds);
                var userAllowed = userKey == null || await _rateLimiter.IsAllowedAsync(userKey, 5, windowSeconds);

                // Step 6 — Reject if either limit exceeded
                if (!ipAllowed || !userAllowed)
                {
                    var limitExceededKey = !ipAllowed ? ipKey : (userKey ?? ipKey);
                    var ttl = await _rateLimiter.GetTtlAsync(limitExceededKey);

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["Retry-After"] = ttl.ToString();
                    context.Response.Headers["X-RateLimit-Limit"] = "5";
                    context.Response.Headers["X-RateLimit-Remaining"] = "0";

                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Too many requests",
                        message = !ipAllowed
                            ? "Too many attempts from your IP. Try again later."
                            : "Too many login attempts for this account. Try again later.",
                        retryAfterSeconds = ttl
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rate limiting check failed for IP {IP}, allowing request (fail-open)", ip);
                // Fail open — if rate limiting fails, allow the request
            }
        }

        await _next(context);
    }

    private static bool IsLoginEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/auth/login");
    }

    private static async Task<string?> ExtractUsernameFromRequestAsync(HttpContext context)
    {
        try
        {
            if (context.Request.Body.CanSeek)
                context.Request.Body.Position = 0;

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (context.Request.Body.CanSeek)
                context.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(body))
                return null;

            var json = System.Text.Json.JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("username", out var prop)
                ? prop.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
