# Processing Service Implementation Guide

## Overview

The **Processing Service** is a critical microservice in the event-driven order processing system. It consumes `OrderPlaced` events from RabbitMQ and performs order validation and execution asynchronously.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    RabbitMQ Message Broker                  │
│                   (order-placed-events queue)               │
└────────────────────────────┬────────────────────────────────┘
                             │
                             │ OrderPlacedEvent (JSON)
                             ↓
         ┌───────────────────────────────────────┐
         │   RabbitConsumerService (Background)  │
         │   - Listens to queue                  │
         │   - Manages message consumption       │
         │   - Handles acknowledgements          │
         └────────────┬────────────────────────┘
                      │
                      ↓
         ┌───────────────────────────────────────┐
         │   OrderPlacedConsumer                 │
         │   - Deserializes event                │
         │   - Orchestrates validation & exec    │
         └────────────┬────────────────────────┘
                      │
         ┌────────────┴────────────┐
         │                         │
         ↓                         ↓
   ┌──────────────┐    ┌──────────────────┐
   │OrderValidator│    │ OrderExecutor    │
   │- Validates   │    │- Executes trade  │
   │- Business    │    │- Updates status  │
   │  rules       │    │- Logs results    │
   └──────────────┘    └──────────────────┘
```

## Project Structure

```
ProcessingService/
│
├── Controllers/
│   └── HealthController.cs              → Health check endpoint
│
├── Events/
│   ├── OrderPlacedEvent.cs              → Event model (consumed)
│   └── OrderProcessedEvent.cs           → Event model (optional, for publishing)
│
├── Consumers/
│   └── OrderPlacedConsumer.cs           → Consumes and orchestrates processing
│
├── Service/
│   ├── OrderValidator.cs                → Validates orders (business rules)
│   └── OrderExecutor.cs                 → Executes trades
│
├── Infrastructure/
│   └── RabbitMqConnection.cs            → RabbitMQ connection factory
│
├── BackgroundService/
│   └── RabbitConsumerService.cs         → IHostedService for message consumption
│
├── Properties/
│   └── launchSettings.json              → Launch configuration
│
├── appsettings.json                     → Configuration (prod)
├── appsettings.Development.json         → Configuration (dev)
├── Program.cs                           → Dependency injection & startup
├── ProcessingService.csproj             → Project file
├── Dockerfile                           → Docker image definition
└── ProcessingService.http               → HTTP testing file
```

## Key Components

### 1. RabbitMqConnection (Infrastructure/RabbitMqConnection.cs)

**Purpose:** Manages RabbitMQ connection lifecycle

**Features:**
- Creates and maintains a single RabbitMQ connection
- Supports automatic recovery with configurable intervals
- Configuration-driven hostname, port, credentials
- Logging for connection events

**Key Methods:**
```csharp
public IModel CreateChannel()    // Create a new channel for publishing/consuming
public void Dispose()             // Cleanup resources
```

**Configuration (appsettings.json):**
```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

---

### 2. OrderValidator (Service/OrderValidator.cs)

**Purpose:** Validates orders against business rules

**Validation Rules:**
1. ✅ Required field checks (OrderId, ClientId, InstrumentSymbol, OrderType)
2. ✅ OrderType validation (must be "BUY" or "SELL")
3. ✅ Quantity validation (must be > 0)
4. ✅ Price validation (must be > 0)
5. ✅ Order size limits (max 100,000 units)
6. ✅ Total order value limits (max 1,000,000 currency units)
7. ✅ Price reasonableness check (max 100,000)

**Returns:** `ValidationResult` with `IsValid` boolean and `Message`

**Example Usage:**
```csharp
var validator = new OrderValidator(logger);
var result = validator.Validate(orderEvent);

if (!result.IsValid)
{
    // Handle validation failure
}
```

---

### 3. OrderExecutor (Service/OrderExecutor.cs)

**Purpose:** Executes validated orders in the trading system

**Execution Flow:**
1. Log execution attempt
2. Check market conditions
3. Check instrument inventory/availability
4. Get current market price
5. For SELL orders, verify client position
6. Record execution with generated ExecutionId

**Market Checks:**
- **Market Conditions:** Verifies market is open/healthy
- **Inventory Check:** Ensures instrument is tradeable
- **Price Check:** Gets current market price with variance
- **Position Check:** For SELL orders, ensures client owns the position

**Returns:** `ExecutionResult` with success status and ExecutionId

**Example Usage:**
```csharp
var executor = new OrderExecutor(logger);
var result = await executor.ExecuteOrderAsync(orderEvent);

if (result.IsSuccessful)
{
    // Order executed successfully
    var executionId = result.ExecutionId;
}
```

---

### 4. OrderPlacedConsumer (Consumers/OrderPlacedConsumer.cs)

**Purpose:** Consumes OrderPlaced events and orchestrates processing

**Workflow:**
1. Receives JSON message from RabbitMQ
2. Deserializes to `OrderPlacedEvent`
3. Validates using `OrderValidator`
4. If invalid, logs warning and returns
5. If valid, executes using `OrderExecutor`
6. If execution fails, logs error
7. If successful, publishes optional `OrderProcessedEvent`

**Key Methods:**
```csharp
public async Task ConsumeAsync(string message)  // Process a message
```

---

### 5. RabbitConsumerService (BackgroundService/RabbitConsumerService.cs)

**Purpose:** Hosted background service that listens to RabbitMQ

**Features:**
- ✅ Implements `IHostedService` for lifecycle management
- ✅ Automatic queue declaration
- ✅ Async event consumption
- ✅ Manual message acknowledgement
- ✅ Message requeuing on failure
- ✅ Graceful shutdown

**Lifecycle Events:**
- `StartAsync()` - Logs startup
- `ExecuteAsync()` - Main consumption loop
- `StopAsync()` - Cleanup and shutdown

**Queue Configuration:**
- Queue Name: `order-placed-events`
- Durable: `true` (survives broker restart)
- Exclusive: `false` (can have multiple consumers)
- Auto Delete: `false` (persists until manually deleted)
- Manual ACK: `true` (reliable message processing)

---

## Dependency Injection (Program.cs)

```csharp
// RabbitMQ Connection
builder.Services.AddSingleton<RabbitMqConnection>();

// Services
builder.Services.AddSingleton<OrderValidator>();
builder.Services.AddSingleton<OrderExecutor>();
builder.Services.AddSingleton<OrderPlacedConsumer>();

// Background Service
builder.Services.AddHostedService<RabbitConsumerService>();

// Logging
builder.Services.AddLogging(config => {
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});
```

---

## Message Format

### OrderPlacedEvent (from OrderService)

```json
{
  "orderId": "ORDER-12345",
  "clientId": "CLIENT-001",
  "instrumentSymbol": "AAPL",
  "orderType": "BUY",
  "quantity": 100,
  "price": 150.50,
  "status": "PLACED",
  "createdAt": "2024-04-14T10:30:00Z"
}
```

### OrderProcessedEvent (published by ProcessingService - optional)

```json
{
  "orderId": "ORDER-12345",
  "clientId": "CLIENT-001",
  "instrumentSymbol": "AAPL",
  "status": "EXECUTED",
  "message": "Order executed at price 150.45",
  "processedAt": "2024-04-14T10:30:05Z"
}
```

---

## Error Handling & Resilience

### Message Failure Handling
```
Message Processing Error
        ↓
    BasicNack
        ↓
    Message Requeued ← Can be consumed again
```

### Validation Failures
- Logged as warnings
- Message acknowledged (removed from queue)
- Optional: Publish rejection event

### Execution Failures
- Logged as errors
- Message acknowledged
- Optional: Publish failure event
- No requeue (prevent infinite loops)

---

## API Endpoints

### Health Check
```http
GET http://localhost:5002/api/health/status
```

**Response:**
```json
{
  "service": "ProcessingService",
  "status": "Healthy",
  "timestamp": "2024-04-14T10:30:00Z",
  "message": "Processing Service is running and listening for OrderPlaced events"
}
```

---

## Configuration

### Development Settings (appsettings.Development.json)
- Log Level: Debug (verbose output)
- RabbitMQ: localhost:5672

### Production Settings (appsettings.json)
- Log Level: Information
- RabbitMQ: Configurable via appsettings or environment variables

### Environment Variables
```bash
RabbitMq__HostName=rabbitmq
RabbitMq__Port=5672
RabbitMq__UserName=guest
RabbitMq__Password=guest
```

---

## Running the Service

### Option 1: Local Development
```bash
cd ProcessingService
dotnet restore
dotnet run
```

Service listens on: `http://localhost:5002`

### Option 2: Docker
```bash
# Build
docker build -t processingservice:latest .

# Run
docker run -p 5002:80 \
  -e RabbitMq__HostName=rabbitmq \
  -e RabbitMq__Port=5672 \
  processingservice:latest
```

### Option 3: Docker Compose
```bash
docker-compose up -d
```

---

## Monitoring & Testing

### RabbitMQ Management Console
- URL: `http://localhost:15672`
- Credentials: guest/guest

**View:**
- Queues: `order-placed-events`
- Consumer: `order-placed-consumer`
- Message rates
- Acknowledgement status

### Logs
```bash
# View logs
dotnet run --configuration=Development

# Or with docker
docker logs trading-processingservice
```

### Test with Sample Event
```json
{
  "orderId": "TEST-001",
  "clientId": "CLIENT-001",
  "instrumentSymbol": "AAPL",
  "orderType": "BUY",
  "quantity": 100,
  "price": 150.50,
  "status": "PLACED",
  "createdAt": "2024-04-14T10:30:00Z"
}
```

Publish to `order-placed-events` queue using RabbitMQ Management Console.

---

## Key Patterns Used

### 1. Event-Driven Architecture
- Asynchronous event consumption
- Decoupled services via message broker

### 2. Background Service Pattern
- `IHostedService` implementation
- Automatic lifecycle management
- Graceful shutdown

### 3. Consumer Pattern
- Single responsibility (consume + orchestrate)
- Delegates to specific services

### 4. Validator Pattern
- Separate validation concerns
- Returns result objects
- Reusable logic

### 5. Executor Pattern
- Encapsulates business logic
- Market checks + execution
- Comprehensive logging

### 6. Result Pattern
- Return success/failure with context
- Avoids exceptions for control flow

---

## Future Enhancements

1. **Persistence**
   - Store executed orders in database
   - Audit trail for compliance

2. **Advanced Validation**
   - Client eligibility checks
   - Portfolio constraints
   - Risk limits

3. **Monitoring**
   - Prometheus metrics
   - Distributed tracing (correlation IDs)
   - Alert thresholds

4. **Dead Letter Queue (DLQ)**
   - Automatic routing for failed messages
   - Manual remediation process
   - Monitoring alerts

5. **Circuit Breaker**
   - Prevent cascade failures
   - Graceful degradation

6. **Retry Policy**
   - Exponential backoff
   - Maximum retry attempts
   - Configurable retry delays

7. **Message Encryption**
   - TLS for RabbitMQ connections
   - Sensitive data protection

---

## Troubleshooting

### Service Won't Start
- Check RabbitMQ is running: `docker ps`
- Verify connection settings in appsettings.json
- Check logs for connection errors

### Messages Not Being Consumed
- Verify queue exists: Check RabbitMQ Management Console
- Check consumer is registered: Look for logs "started and waiting for messages"
- Verify OrderService is publishing events

### Validation Failures
- Check OrderPlacedEvent schema matches expected format
- Verify all required fields are present
- Review business rule thresholds in OrderValidator.cs

### Execution Failures
- Check market conditions (currently always healthy)
- Review inventory checks in OrderExecutor.cs
- Check price variance logic

---

## Dependencies

```xml
<PackageReference Include="RabbitMQ.Client" Version="6.8.1"/>
<PackageReference Include="System.Text.Json" Version="4.7.2"/>
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11"/>
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2"/>
```

---

## Related Services

- **Order Service**: Publishes `OrderPlaced` events
- **RabbitMQ**: Message broker
- **Shared Library**: Common event models (optional)

---

## Contact & Support

For issues or questions:
1. Check logs in `ProcessingService` console output
2. Verify RabbitMQ connectivity
3. Review this implementation guide
4. Check readme.md for architecture overview

