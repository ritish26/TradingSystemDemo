using Microsoft.AspNetCore.Http;
using Shared.Application.Interfaces;
using Shared.Domain.Exceptions;

namespace Shared.Infrastructure.API.ExceptionStrategies;

// Handles ForbiddenException. Returns 403 Forbidden status.
// Follows SRP - single strategy for one exception type.
public class ForbiddenExceptionStrategy : IExceptionResponseStrategy
{
    public bool CanHandle(Exception exception)
    {
        return exception is ForbiddenException;
    }

    public void Handle(Exception exception, HttpContext context)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
