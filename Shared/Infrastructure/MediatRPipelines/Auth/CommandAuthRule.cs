namespace Shared.Infrastructure.MediatRPipelines.Auth;

public class CommandAuthRule
{
    public string[]? Roles    { get; init; }
    public string[]? Policies { get; init; }
}