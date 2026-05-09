using Microsoft.AspNetCore.Http;

namespace OrderService.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiting middleware for order creation endpoints.
/// Uses client ID (from JWT claims) as the rate limit key.
/// Prevents abuse and ensures fair resource allocation across instances.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisRateLimiter _rateLimiter;

    public RateLimitingMiddleware(RequestDelegate next, RedisRateLimiter rateLimiter)
    {
        _next = next;
        _rateLimiter = rateLimiter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsRateLimitedEndpoint(context.Request.Path))
        {
            var clientId = context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString();
            if (clientId != null && !await _rateLimiter.IsAllowedAsync(clientId))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
                return;
            }
        }

        await _next(context);
    }

    private static bool IsRateLimitedEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/order/create");
    }
}
