namespace Shared.Application.Interfaces;

public interface IVaultTokenProvider
{
    Task<string> GetTokenAsync();
}
