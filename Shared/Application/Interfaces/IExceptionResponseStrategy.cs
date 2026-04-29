using Microsoft.AspNetCore.Http;

namespace Shared.Application.Interfaces;

// Strategy pattern for exception response handling. Decouples exception types from response generation.
// Follows OCP - adding new exception types doesn't require modifying existing strategies.
// Single responsibility - each strategy handles one exception type.
public interface IExceptionResponseStrategy
{
    void Handle(Exception exception, HttpContext context);
    bool CanHandle(Exception exception);
}
