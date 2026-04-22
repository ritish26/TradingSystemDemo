using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.Helper;

namespace Shared.Infrastructure.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyStore _store; 
    
    public IdempotencyMiddleware(IIdempotencyStore store, RequestDelegate next)
    {
        _store = store;
        _next = next;
    }
    
    public async Task Invoke(HttpContext context)
    {
        if (!context.Items.TryGetValue("IdempotencyKey", out var keyObj))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyObj.ToString();

        if (idempotencyKey != null && _store.TryGet(idempotencyKey, out var cachedResponse))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(cachedResponse);
            return;
        }

        var originalBody = context.Response.Body;

        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await _next(context);

        memStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();

        if (idempotencyKey != null) _store.Save(idempotencyKey, responseBody);

        memStream.Seek(0, SeekOrigin.Begin);
        await memStream.CopyToAsync(originalBody);
    }
}