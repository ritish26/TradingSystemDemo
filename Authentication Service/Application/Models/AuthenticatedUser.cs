namespace Authentication_Service.Application.Models;

public sealed class AuthenticatedUser
{
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
