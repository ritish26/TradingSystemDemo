using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.API.ExceptionHandling;

namespace Shared.API.Middleware;

// Middleware that delegates exception handling to ExceptionHandler using strategy pattern.
// Decouples specific exception handling from middleware - easier to test and extend.
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ExceptionHandler _exceptionHandler;

    public ExceptionMiddleware(RequestDelegate next, ExceptionHandler exceptionHandler)
    {
        _next = next;
        _exceptionHandler = exceptionHandler;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex, context);
        }
    }
}