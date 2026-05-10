using Authentication_Service.Infrastructure.RateLimiting;
using StackExchange.Redis;

namespace Authentication_Service.Infrastructure.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRedisRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        var redis = ConnectionMultiplexer.Connect(redisConnection!);

        // Register as interface for dependency injection (required by RedisWhitelistService)
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton(redis);
        services.AddSingleton(sp =>
            new RedisRateLimiter(
                redis,
                maxRequests: int.Parse(configuration["RateLimiting:MaxRequests"] ?? "5"),
                windowSeconds: int.Parse(configuration["RateLimiting:WindowSeconds"] ?? "300")
            )
        );

        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        app.UseMiddleware<RateLimitingMiddleware>();
        return app;
    }
}
