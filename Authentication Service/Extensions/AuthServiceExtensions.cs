using Authentication_Service.Application.Interfaces;
using Authentication_Service.Configuration;
using Authentication_Service.Infrastructure.Claims;
using Authentication_Service.Infrastructure.Services;

namespace Authentication_Service.Extensions;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings"));

        services.AddSingleton<IRsaKeyProvider, RsaKeyProvider>();

        services.AddSingleton<IUserClaimsProvider, AdminClaimsProvider>();
        services.AddSingleton<IUserClaimsProvider, DefaultClaimsProvider>();

        services.AddSingleton<ITokenService, TokenService>();

        services.AddSingleton<InMemoryUserRepository>();
        services.AddSingleton<IUserAuthenticationService, UserAuthenticationService>();

        return services;
    }
}
