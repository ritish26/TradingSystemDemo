namespace Shared.Infrastructure.MediatRPipelines.Auth;

public interface ICommandAuthorizationRegistry
{
    CommandAuthRule? GetRule(Type commandType);
}