using System.Security.Cryptography;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Configuration;
using Microsoft.Extensions.Options;

namespace Authentication_Service.Infrastructure.Services;

public sealed class RsaKeyProvider : IRsaKeyProvider
{
    private readonly RSA _rsa;

    public RsaKeyProvider(IOptions<JwtSettings> jwtSettings)
    {
        var settings = jwtSettings.Value;
        var privateKeyPem = File.ReadAllText(
            Path.Combine(Directory.GetCurrentDirectory(), settings.PrivateKeyPath));

        _rsa = RSA.Create();
        _rsa.ImportFromPem(privateKeyPem.ToCharArray());
    }

    public RSA GetPrivateKey() => _rsa;
}
