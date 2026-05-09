using Microsoft.AspNetCore.Http;
using Shared.Application.Interfaces;
using Shared.Domain.Exceptions;

namespace Shared.Infrastructure.API.ExceptionStrategies;

public class ConflictExceptionStrategy : IExceptionResponseStrategy
{
    public bool CanHandle(Exception exception)
    {
        return exception is ConflictException;
    }

    public void Handle(Exception exception, HttpContext context)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status409Conflict;
    }
}
