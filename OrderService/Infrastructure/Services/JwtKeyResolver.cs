using Microsoft.IdentityModel.Tokens;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Services;

public sealed class JwtKeyResolver
{
    private readonly IServiceScopeFactory _scopeFactory;

    public JwtKeyResolver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IEnumerable<SecurityKey> ResolveKey(
        string token,
        SecurityToken securityToken,
        string kid,
        TokenValidationParameters validationParameters)
    {
        Console.WriteLine($"JwtKeyResolver called with kid: {kid}");

        using var scope = _scopeFactory.CreateScope();
        var publicKeyProvider = scope.ServiceProvider.GetRequiredService<IPublicKeyProvider>();
        var key = publicKeyProvider.GetPublicKeyAsync(kid).GetAwaiter().GetResult();

        Console.WriteLine($"JwtKeyResolver successfully resolved key for kid: {kid}");
        return [key];
    }
}
