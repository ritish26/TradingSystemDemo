namespace Shared.Infrastructure.CustomException;

public class ForbiddenException : Exception
{
    public ForbiddenException() { }

    public ForbiddenException(string message) : base(message) { }
}
