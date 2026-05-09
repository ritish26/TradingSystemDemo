using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Infrastructure.Extensions;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Services;
using Serilog;
using Shared.API.Middleware;
using Shared.Application.Interfaces;
using Shared.Application.Pipelines.Auth;
using Shared.Application.Pipelines.Validator;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Services", "OrderService");
});

// configure postgres connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")!));
builder.Services.AddDatabaseInitialization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Idempotency store for caching duplicate request responses
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

// Rate limiting: Redis-based for horizontal scaling
builder.Services.AddRedisRateLimiting(builder.Configuration);

// Register shared infrastructure (Configuration, Database, Messaging, ExceptionHandling)
builder.Services.AddSharedInfrastructure();

// Facade: Register all Order Service dependencies using extension method
builder.Services.AddOrderServiceDependencies(builder.Configuration);

// Pipeline behaviors — order matters, auth runs first, then validation
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JWT Auth — key resolution via Vault Transit
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtKeyResolver>((options, resolver) =>
        options.TokenValidationParameters.IssuerSigningKeyResolver = resolver.ResolveKey);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("CanCreateOrder", policy =>
        policy.RequireClaim("permission", "order:create"));
    options.AddPolicy("CanViewOrder", policy =>
        policy.RequireClaim("permission", "order:read"));
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
    await database.InitializeAsync();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCorrelationId();
app.UseRateLimiting();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<IdempotencyKeyGeneratorMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

app.Run();