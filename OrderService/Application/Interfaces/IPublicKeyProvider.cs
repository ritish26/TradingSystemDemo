using Microsoft.IdentityModel.Tokens;

namespace OrderService.Application.Interfaces;

public interface IPublicKeyProvider
{
    Task<RsaSecurityKey> GetPublicKeyAsync(string kid);
}
