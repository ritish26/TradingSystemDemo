using ProcessingService.Infrastructure;
using ProcessingService.Service;
using ProcessingService.Consumers;
using ProcessingService.BackgroundService;

var builder = WebApplication.CreateBuilder(args);

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

// Register Background Service for consuming RabbitMQ messages
builder.Services.AddHostedService<RabbitConsumerService>();

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

app.Run();
