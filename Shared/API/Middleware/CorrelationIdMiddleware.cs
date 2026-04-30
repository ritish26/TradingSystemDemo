using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Shared.Application.Common;
using Shared.Domain.Constants;

namespace Shared.API.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;  
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ExtractCorrelationId(context.Request.Headers);

        CorrelationIdContext.SetCorrelationId(correlationId);

        // attach to ALL logs automatically
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // send back in response header
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append(Constants.CorrelationIdHeaderName, correlationId);
                return Task.CompletedTask;
            });

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

    private string ExtractCorrelationId(IHeaderDictionary headers)
    {
        return headers.TryGetValue(Constants.CorrelationIdHeaderName, out var correlationId)
            ? correlationId.ToString()
            : Guid.NewGuid().ToString("N");
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        => builder.UseMiddleware<CorrelationIdMiddleware>();
}