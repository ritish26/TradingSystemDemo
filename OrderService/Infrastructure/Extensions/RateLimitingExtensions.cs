using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.RateLimiting;
using StackExchange.Redis;

namespace OrderService.Infrastructure.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRedisRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        var redis = ConnectionMultiplexer.Connect(redisConnection!);

        // Register as interface for dependency injection
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton(redis);
        services.AddSingleton(sp =>
            new RedisRateLimiter(
                redis,
                maxRequests: int.Parse(configuration["RateLimiting:MaxRequests"] ?? "100"),
                windowSeconds: int.Parse(configuration["RateLimiting:WindowSeconds"] ?? "60")
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
