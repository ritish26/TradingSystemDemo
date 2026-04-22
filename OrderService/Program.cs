using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderService.AuthorizationRegistry;
using OrderService.Command;
using OrderService.Command.Mapper;
using OrderService.Service;
using Serilog;
using Shared.Infrastructure.Helper;
using Shared.Infrastructure.MediatRPipelines.Auth;
using Shared.Infrastructure.MediatRPipelines.Validator;
using Shared.Infrastructure.Middleware;
using Shared.Infrastructure.RabbitMqConnection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "OrderService");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>(); 


builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<OrderCreatedCommandHandler>();
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// RabbitMQ
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<OrderPublisher>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Registry
builder.Services.AddSingleton<ICommandAuthorizationRegistry, CommandAuthorizationRegistry>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<OrderCreatedCommandValidator>();

// Pipeline behaviors — order matters, auth runs first
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

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

app.UseMiddleware<IdempotencyKeyGeneratorMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();            
app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCorrelationId();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();