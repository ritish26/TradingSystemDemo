using ProcessingService.Infrastructure.Extensions;
using Serilog;
using Shared.API.Middleware;
using Shared.Infrastructure.Extensions;

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

// Register shared infrastructure (Configuration, Database, Messaging, ExceptionHandling)
builder.Services.AddSharedInfrastructure();

// Facade: Register all Processing Service dependencies
builder.Services.AddProcessingServiceDependencies();


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
