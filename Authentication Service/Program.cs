using Authentication_Service.Extensions;
using Authentication_Service.Infrastructure.Extensions;
using Authentication_Service.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Rate limiting: Redis-based for brute force protection across instances
builder.Services.AddRedisRateLimiting(builder.Configuration);

builder.Services.AddAuthenticationServices(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreateOrder", policy =>
        policy.RequireClaim("permission", "order:create"));
    options.AddPolicy("CanViewOrder", policy =>
        policy.RequireClaim("permission", "order:read"));
});

var app = builder.Build();

// Seed initial whitelist from configuration on startup
using (var scope = app.Services.CreateScope())
{
    var whitelistService = scope.ServiceProvider.GetRequiredService<IWhitelistService>();
    var initialIps = builder.Configuration
        .GetSection("RateLimiting:InitialWhitelist")
        .Get<string[]>() ?? Array.Empty<string>();

    foreach (var ip in initialIps)
    {
        await whitelistService.AddToWhitelistAsync(ip);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRateLimiting();

app.UseRouting();
app.MapControllers();
app.Run();