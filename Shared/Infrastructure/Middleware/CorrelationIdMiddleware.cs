using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Shared.Infrastructure.Helper;

namespace Shared.Infrastructure.Middleware;

/// <summary>
/// CorrelationIdMiddleware - HTTP middleware that extracts or generates correlation IDs
/// and makes them available throughout the request pipeline.
/// This enables request tracing across microservices.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to extract correlation ID from request headers
        var correlationId = ExtractCorrelationId(context.Request.Headers);
        
        // Set it in the context
        CorrelationIdContext.SetCorrelationId(correlationId);
        
        // Add it to Serilog LogContext so it's included in all logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Add correlation ID to response headers
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(Constant.Constant.CorrelationIdHeaderName))
                {
                    context.Response.Headers.Append(Constant.Constant.CorrelationIdHeaderName, correlationId);
                }
                return Task.CompletedTask;
            });

            _logger.LogDebug("CorrelationId: {CorrelationId}", correlationId);

            try
            {
                await _next(context);
            }
            finally
            {
                CorrelationIdContext.Clear();
            }
        }
    }

    /// <summary>
    /// Extracts the correlation ID from request headers or generates a new one
    /// </summary>
    private string ExtractCorrelationId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(Constant.Constant.CorrelationIdHeaderName, out var correlationId))
        {
            return correlationId.ToString();
        }

        // Generate a new correlation ID if not present
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension method to add CorrelationIdMiddleware to the pipeline
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}

