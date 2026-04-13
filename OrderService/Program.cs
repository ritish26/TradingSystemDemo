using OrderService2.Messaging;
using OrderService2.Command;
using OrderService2.Service;
using OrderService2.BackgroundServices;
using FluentValidation;
using OrderService2.Request;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add controllers
builder.Services.AddControllers();

// Register RabbitMQ Connection
builder.Services.AddSingleton<RabbitMqConnection>();

// Register Publishers and Handlers
builder.Services.AddSingleton<CommandPublisher>();
builder.Services.AddSingleton<OrderPublisher>();
builder.Services.AddSingleton<OrderCreatedCommandHandler>();

// Register Background Service
builder.Services.AddHostedService<CommandConsumerService>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// Register Fluent Validation
builder.Services.AddValidatorsFromAssemblyContaining<OrderRequestValidator>();

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();

app.Run();