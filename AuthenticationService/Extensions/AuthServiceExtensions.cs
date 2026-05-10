using AuthenticationService.Application.Interfaces;
using AuthenticationService.Configuration;
using AuthenticationService.Infrastructure.Claims;
using AuthenticationService.Infrastructure.RateLimiting;
using AuthenticationService.Infrastructure.Services;
using Shared.Application.Interfaces;
using Shared.Configuration;
using Shared.Infrastructure.Services;

namespace AuthenticationService.Extensions;

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
