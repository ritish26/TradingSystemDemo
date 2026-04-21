using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.CustomException;

namespace Shared.Infrastructure.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.Clear(); 
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        catch (ForbiddenException)
        {
            context.Response.Clear(); 
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
    }
}