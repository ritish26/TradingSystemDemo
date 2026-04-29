using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Infrastructure.Extensions;
using OrderService.Infrastructure.Persistence;
using Serilog;
using Shared.API.Middleware;
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
builder.Services.AddDatabaseInitialization(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

// Facade: Register all Order Service dependencies using extension method
builder.Services.AddOrderServiceDependencies();

// Pipeline behaviors — order matters, auth runs first
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JWT Auth — make sure this is configured
var publicKeyPath = builder.Configuration["JwtSettings:PublicKeyPath"];

var publicKeyPem = File.ReadAllText(
    Path.Combine(AppContext.BaseDirectory, publicKeyPath!));

var rsa = RSA.Create();

rsa.ImportFromPem(publicKeyPem.ToCharArray());

var rsaSecurityKey = new RsaSecurityKey(rsa);

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
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaSecurityKey
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreateOrder", policy =>
        policy.RequireClaim("permission", "order:create"));
    options.AddPolicy("CanViewOrder", policy =>
        policy.RequireClaim("permission", "order:read"));
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await dbInitializer.InitializeAsync();
}

// app.UseMiddleware<IdempotencyKeyGeneratorMiddleware>();
// app.UseMiddleware<IdempotencyMiddleware>();            
app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCorrelationId();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();