namespace Shared.Application.Pipelines.Auth;

public class CommandAuthRule
{
    public string[]? Roles    { get; init; }
    public string[]? Policies { get; init; }
}