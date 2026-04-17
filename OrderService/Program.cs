using OrderService2.Command;
using OrderService2.Service;
using OrderService2.Mediator;
using FluentValidation;
using OrderService2.Request;
using Shared.Infrastructure;
using Serilog;

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

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add controllers
builder.Services.AddControllers();

// Register RabbitMQ Connection
builder.Services.AddSingleton<RabbitMqConnection>();

// Register Publishers and Handlers
builder.Services.AddSingleton<OrderPublisher>();
builder.Services.AddSingleton<OrderCreatedCommandHandler>();

// Register Command Mediator
builder.Services.AddSingleton<ICommandMediator, CommandMediator>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// Register Fluent Validation
builder.Services.AddValidatorsFromAssemblyContaining<OrderRequestValidator>();


var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Add Correlation ID middleware for request tracking
app.UseCorrelationId();

app.UseRouting();
app.MapControllers();

app.Run();