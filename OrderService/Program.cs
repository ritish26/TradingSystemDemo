using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderService.AuthorizationRegistry;
using OrderService.Command;
using OrderService.Command.Mapper;
using OrderService.Service;
using Serilog;
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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreateOrder", policy =>
        policy.RequireClaim("permission", "order:create"));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCorrelationId();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();