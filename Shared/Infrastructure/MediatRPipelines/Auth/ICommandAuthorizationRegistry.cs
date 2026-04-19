namespace Shared.Infrastructure.MediatRPipelines.Auth;

// Shared/ICommandAuthorizationRegistry.cs
public interface ICommandAuthorizationRegistry
{
    CommandAuthRule? GetRule(Type commandType);
}