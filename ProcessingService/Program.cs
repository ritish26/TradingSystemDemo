using ProcessingService.Infrastructure.Messaging;
using ProcessingService.Infrasturcture.Services;
using Serilog;
using Shared.API.Middleware;
using Shared.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Services", "ProcessingService");
});

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add controllers
builder.Services.AddControllers();

// Register RabbitMQ Connection
builder.Services.AddSingleton<RabbitMqConnection>();

// Register Services
builder.Services.AddSingleton<OrderValidator>();
builder.Services.AddSingleton<OrderExecutor>();
builder.Services.AddSingleton<OrderPlacedConsumer>();

// Register Background Services for consuming RabbitMQ messages
builder.Services.AddHostedService<RabbitConsumerService>();


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationId();

app.UseRouting();
app.MapControllers();

app.Run();
