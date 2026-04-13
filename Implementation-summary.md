# Order Service - Command-Based Architecture Implementation

## Overview
The Order Service has been implemented with a **Command-Query Responsibility Segregation (CQRS)** pattern combined with **event-driven architecture** using RabbitMQ for async communication.

## Architecture Flow

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ POST /api/order/create
       ▼
┌──────────────────────────┐
│   OrderController        │
│  - Receives order        │
│  - Creates OrderCommand  │
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│  CommandPublisher        │
│  - Sends command to queue│
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│    RabbitMQ              │
│  Queue: order-created    │
│         -commands        │
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│ CommandConsumerService   │
│ - Background Service     │
│ - Listens to commands    │
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│ CommandHandler           │
│ - Processes command      │
│ - Validates              │
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│  OrderPublisher          │
│  - Publishes event       │
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│    RabbitMQ              │
│  Queue: order-placed     │
│         -events          │
└──────┬───────────────────┘
       │
       ▼
  (RiskService consumes)
```

## Components Implemented

### 1. **Models & Commands**

#### OrderRequest.cs
- API request model from client
- Properties: ClientId, InstrumentSymbol, OrderType, Quantity, Price

#### OrderCreatedCommand.cs
- Command object for CQRS pattern
- Extends OrderRequest with OrderId and CreatedAt timestamp

#### OrderPlacedEvent.cs
- Event published after command processing
- Consumed by RiskService
- Status tracking: "PLACED"

### 2. **Messaging Infrastructure**

#### RabbitMqConnection.cs
- Singleton service managing RabbitMQ connection
- Connection pooling with automatic recovery
- Configurable via appsettings.json
- Methods:
  - `CreateChannel()` - Returns IModel for publishing/consuming

#### CommandPublisher.cs
- Publishes OrderCreatedCommand to queue
- Queue: `order-created-commands`
- Durable messages with persistence
- Serializes commands to JSON

#### OrderPublisher.cs
- Publishes OrderPlacedEvent to queue
- Queue: `order-placed-events`
- Consumed by RiskService
- Durable event storage

### 3. **Command Processing**

#### OrderCreatedCommandHandler.cs
- Handles OrderCreatedCommand
- Business logic:
  - Validates command data
  - Creates OrderPlacedEvent
  - Publishes event to RiskService queue
- Logging at each step

#### CommandConsumerService.cs (Background Service)
- Hosted service running in background
- Listens to `order-created-commands` queue
- Processes commands asynchronously
- Features:
  - Fair dispatch (QoS = 1)
  - Manual acknowledgment
  - Error handling with requeue
  - Graceful shutdown

### 4. **API Controller**

#### OrderController.cs
- **POST /api/order/create** - Create new trade order
  - Accepts OrderRequest
  - Generates unique OrderId (GUID)
  - Publishes command to queue
  - Returns 202 Accepted (fire-and-forget pattern)
  - Returns: `{ orderId, status: "PENDING", message: "..." }`
  
- **GET /api/order/health** - Health check
  - Returns service status

## Configuration

### appsettings.json
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

### Dependency Injection (Program.cs)
```csharp
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<CommandPublisher>();
builder.Services.AddSingleton<OrderPublisher>();
builder.Services.AddSingleton<OrderCreatedCommandHandler>();
builder.Services.AddHostedService<CommandConsumerService>();
```

## NuGet Dependencies Added
- **RabbitMQ.Client** (v6.8.1) - Message broker client
- **System.Text.Json** (v4.7.2) - JSON serialization

## Queue Design

### order-created-commands
- **Purpose**: Command queue for intake
- **Durable**: Yes (survives restarts)
- **Type**: Direct/Point-to-Point
- **Consumer**: CommandConsumerService (internal)
- **Message Format**: JSON serialized OrderCreatedCommand

### order-placed-events
- **Purpose**: Event queue for downstream services
- **Durable**: Yes
- **Type**: Direct/Point-to-Point
- **Consumers**: RiskService
- **Message Format**: JSON serialized OrderPlacedEvent

## Design Patterns Used

1. **CQRS (Command-Query Responsibility Segregation)**
   - Commands: OrderCreatedCommand
   - Queries: Not implemented (future)

2. **Event-Driven Architecture**
   - Commands trigger events
   - Services communicate via events
   - Loose coupling between services

3. **Async/Fire-and-Forget**
   - Client gets immediate 202 Accepted response
   - Processing happens asynchronously
   - No blocking operations

4. **Background Service Pattern**
   - CommandConsumerService as hosted service
   - Runs continuously during app lifetime
   - Graceful start/stop

5. **Singleton Pattern**
   - RabbitMQ connection shared across requests
   - Reduces connection overhead

## Error Handling

- **Command Publishing Failures**: Logged and thrown
- **Command Processing Failures**: Logged, message nack'd and requeued
- **Connection Failures**: Automatic recovery enabled
- **Serialization Errors**: Logged and nack'd

## Testing the Service

### Health Check
```bash
curl http://localhost:5001/api/order/health
```

### Create Order
```bash
curl -X POST http://localhost:5001/api/order/create \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "CLIENT123",
    "instrumentSymbol": "AAPL",
    "orderType": "BUY",
    "quantity": 100,
    "price": 150.50
  }'
```

### Response
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "PENDING",
  "message": "Order command published for processing"
}
```

## Files Structure
```
OrderService/
├── BackgroundServices/
│   └── CommandConsumerService.cs ✅
├── Command/
│   ├── OrderCreatedCommand.cs ✅
│   └── OrderCreatedCommandHandler.cs ✅
├── Controller/
│   └── OrderController.cs ✅
├── Event/
│   └── OrderPlacedEvent.cs ✅
├── Messaging/
│   ├── RabbitMqConnection.cs ✅
│   └── CommandPublisher.cs ✅
├── Model/
│   └── OrderRequest.cs ✅
├── Service/
│   └── OrderPublisher.cs ✅
├── appsettings.json ✅
├── Dockerfile ✅
├── OrderService.csproj ✅
└── Program.cs ✅
```

## Next Steps

1. **Implement RiskService**
   - Consume OrderPlacedEvent
   - Validate trades
   - Publish RiskApprovedEvent

2. **Implement ExecutionService**
   - Consume RiskApprovedEvent
   - Execute trades
   - Publish TradeExecutedEvent

3. **Docker Deployment**
   - Build: `docker-compose build`
   - Run: `docker-compose up`

4. **Enhancements**
   - Add order tracking/persistence
   - Implement saga pattern for distributed transactions
   - Add retry policies
   - Add dead-letter queues (DLQ)
   - Add circuit breakers

