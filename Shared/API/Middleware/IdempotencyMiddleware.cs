using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Shared.API.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyStore _store;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(IIdempotencyStore store, RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _store = store;
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Skip idempotency caching for GET requests (read-only operations)
        if (context.Request.Method == "GET")
        {
            await _next(context);
            return;
        }

        if (!context.Items.TryGetValue("IdempotencyKey", out var keyObj))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyObj?.ToString();

        // SECURITY: Authentication & Authorization have already run.
        // Idempotency key includes userId, preventing cross-user cache hits.
        if (!string.IsNullOrEmpty(idempotencyKey) && _store.TryGet(idempotencyKey, out var cachedData))
        {
            try
            {
                var cached = JsonDocument.Parse(cachedData).RootElement;
                int statusCode = cached.GetProperty("StatusCode").GetInt32();
                string body = cached.GetProperty("Body").GetString() ?? "";

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(body);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError("Idempotency cache parse failed: {Error}", ex.Message);
            }
        }

        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        try
        {
            await _next(context);

            memStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memStream).ReadToEndAsync();

            // Cache only successful responses (2xx status codes)
            if (!string.IsNullOrEmpty(idempotencyKey) && context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                var cacheEntry = new { StatusCode = context.Response.StatusCode, Body = responseBody };
                var cacheJson = JsonSerializer.Serialize(cacheEntry);
                _store.Save(idempotencyKey, cacheJson);
            }

            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
