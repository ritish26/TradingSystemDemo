using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Shared.API.Middleware;

public class IdempotencyKeyGeneratorMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyKeyGeneratorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // If client already sent key → use it
        if (context.Request.Headers.ContainsKey("Infrastructure-Key"))
        {
            context.Items["IdempotencyKey"] = context.Request.Headers["Infrastructure-Key"].ToString();
            await _next(context);
            return;
        }

        // Read request body
        context.Request.EnableBuffering();

        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Generate deterministic key (clientId + instrumentId)
        var key = GenerateKey(body);

        context.Items["IdempotencyKey"] = key;

        await _next(context);
    }

    private static string GenerateKey(string body)
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
            catch (JsonException ex)
            {
                Console.WriteLine($"[IdempotencyKeyGenerator] JSON parse failed: {ex.Message}");
            }
        }

        clientId ??= "unknown-client";
        instrumentId ??= "unknown-instrument";

        return $"idmp-{clientId}-{instrumentId}";
    }
}