using Authentication_Service.Application.Interfaces;
using Authentication_Service.Configuration;
using Authentication_Service.Infrastructure.Claims;
using Authentication_Service.Infrastructure.RateLimiting;
using Authentication_Service.Infrastructure.Services;
using Shared.Application.Interfaces;
using Shared.Configuration;
using Shared.Infrastructure.Services;

namespace Authentication_Service.Extensions;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings"));
        services.Configure<VaultSettings>(
            configuration.GetSection("Vault"));

        services.AddHttpClient<IVaultTokenProvider, VaultTokenProvider>();
        services.AddScoped<ITransitSigner, VaultTransitSigner>();

        services.AddSingleton<IUserClaimsProvider, AdminClaimsProvider>();
        services.AddSingleton<IUserClaimsProvider, DefaultClaimsProvider>();

        services.AddScoped<ITokenService, TokenService>();

        services.AddSingleton<InMemoryUserRepository>();
        services.AddSingleton<IUserAuthenticationService, UserAuthenticationService>();

        services.AddSingleton<IWhitelistService, RedisWhitelistService>();

        services.AddSingleton<ITokenCacheService, RedisTokenCacheService>();

        return services;
    }
}
