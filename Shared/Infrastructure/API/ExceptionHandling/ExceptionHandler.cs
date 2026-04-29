using Microsoft.AspNetCore.Http;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.API.ExceptionHandling;

// Factory that routes exceptions to appropriate strategy handlers.
// Follows Strategy pattern - decouples exception handling from middleware.
// Easy to add new exception types without modifying middleware code (OCP).
public class ExceptionHandler
{
    private readonly IEnumerable<IExceptionResponseStrategy> _strategies;

    public ExceptionHandler(IEnumerable<IExceptionResponseStrategy> strategies)
    {
        _strategies = strategies;
    }

    public void Handle(Exception exception, HttpContext context)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(exception));

        if (strategy != null)
        {
            strategy.Handle(exception, context);
        }
    }
}
