using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.API.Middleware;

public class IdempotencyKeyGeneratorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyKeyGeneratorMiddleware> _logger;

    public IdempotencyKeyGeneratorMiddleware(RequestDelegate next, ILogger<IdempotencyKeyGeneratorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // If client already sent key → use it
        if (context.Request.Headers.ContainsKey("Infrastructure-Key"))
        {
            var clientKey = context.Request.Headers["Infrastructure-Key"].ToString();
            context.Items["IdempotencyKey"] = clientKey;
            await _next(context);
            return;
        }

        // Read request body
        context.Request.EnableBuffering();

        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Generate deterministic key (userId + clientId + instrumentId)
        var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
        var key = GenerateKey(body, userId);
        context.Items["IdempotencyKey"] = key;

        await _next(context);
    }

    private static string GenerateKey(string body, string userId)
    {
        string? clientId = null;
        string? instrumentId = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    // Case-insensitive search across all properties
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name.Equals("clientId", StringComparison.OrdinalIgnoreCase))
                            clientId = prop.Value.GetString();

                        if (prop.Name.Equals("instrumentSymbol", StringComparison.OrdinalIgnoreCase))
                            instrumentId = prop.Value.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Continue with unknown values if JSON parsing fails
            }
        }

        clientId ??= "unknown-client";
        instrumentId ??= "unknown-instrument";

        return $"idmp-{userId}-{clientId}-{instrumentId}";
    }
}