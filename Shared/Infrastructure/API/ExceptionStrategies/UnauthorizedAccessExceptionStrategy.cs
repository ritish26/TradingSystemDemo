using Microsoft.AspNetCore.Http;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.API.ExceptionStrategies;

// Handles UnauthorizedAccessException. Returns 401 Unauthorized status.
// Follows SRP - single strategy for one exception type.
public class UnauthorizedAccessExceptionStrategy : IExceptionResponseStrategy
{
    public bool CanHandle(Exception exception)
    {
        return exception is UnauthorizedAccessException;
    }

    public void Handle(Exception exception, HttpContext context)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }
}
