namespace Authentication_Service.Application.Interfaces;

public interface IVaultTokenProvider
{
    Task<string> GetTokenAsync();
}
