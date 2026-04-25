using Shared.Application.Pipelines.Auth;

namespace Shared.Application.Interfaces;

public interface ICommandAuthorizationRegistry
{
    CommandAuthRule? GetRule(Type commandType);
}