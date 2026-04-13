```csharp
Design and implement an event-driven trade lifecycle processing system for a wealth management platform.

When a client places an investment order (buy/sell of financial instruments), the system should process the trade asynchronously across multiple independent microservices.

The Order Service captures the trade request and publishes an OrderPlaced event to a message broker. A Risk & Compliance Service consumes this event and validates the trade against regulatory rules, client eligibility, and portfolio constraints. Upon successful validation, it publishes a TradeApproved event. The Execution Service then consumes the approved event and executes the trade.

The system should be loosely coupled, scalable, and resilient, ensuring that failures in downstream services do not impact order intake and that all trade events are reliably processed.
```

Flow:
```csharp
Client places trade
   ↓
Order Service → OrderPlacedEvent
   ↓
RabbitMQ
   ↓
Risk & Compliance Service → validation
   ↓
TradeApprovedEvent
   ↓
RabbitMQ
   ↓
Execution Service → executes trade
```

Project Structure:
```
FNZTradeSystem/
│
├── FNZTradeSystem.sln
│
├── OrderService/                🟢 (Trade Intake)
├── RiskService/                 🔵 (Risk + Compliance)
├── ExecutionService/            🟣 (Trade Execution)
│
├── Shared/                      📦 (Common contracts)
│
└── docker-compose.yml           🐳 (Run everything)
```

### Key Components:
1. Order Service: Handles incoming trade requests and publishes OrderPlaced events.
```csharp
OrderService/
│
├── Controllers/
│   └── OrderController.cs       → POST /order
│
├── Services/
│   └── OrderPublisher.cs        → Publish event to RabbitMQ
│
├── Models/
│   └── OrderRequest.cs          → API request model
│
├── Events/
│   └── OrderPlacedEvent.cs      → Event (can also come from Shared)
│
├── Messaging/
│   └── RabbitMqConnection.cs
│
├── appsettings.json
└── Program.cs
```

1. RiskService:

```csharp
RiskService/
│
├── Consumers/
│   └── OrderPlacedConsumer.cs   → Listen to order-placed queue
│
├── Services/
│   └── RiskValidator.cs         → Business logic (limits, rules)
│
├── Events/
│   └── RiskApprovedEvent.cs
│
├── Messaging/
│   └── RabbitMqConnection.cs
│
├── BackgroundServices/
│   └── RabbitConsumerService.cs → Hosted service
│
├── appsettings.json
└── Program.cs
```

3. ExecutionService:
```csharp
ExecutionService/
│
├── Consumers/
│   └── RiskApprovedConsumer.cs  → Listen to risk-approved queue
│
├── Services/
│   └── TradeExecutor.cs         → Execute trade
│
├── Messaging/
│   └── RabbitMqConnection.cs
│
├── BackgroundServices/
│   └── RabbitConsumerService.cs
│
├── appsettings.json
└── Program.cs
```

### Shared Library:
```csharp
Shared/
│
├── Events/
│   ├── OrderPlacedEvent.cs
│   ├── RiskApprovedEvent.cs
│
├── Constants/
│   └── QueueNames.cs
│
└── DTOs/
    └── CommonModels.cs
```
### Docker Compose:
```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"

  orderservice:
    build: ./OrderService
    ports:
      - "5001:80"
    depends_on:
      - rabbitmq

  riskservice:
    build: ./RiskService
    ports:
      - "5002:80"
    depends_on:
      - rabbitmq

  executionservice:
    build: ./ExecutionService
    ports:
      - "5003:80"
    depends_on:
      - rabbitmq
```

### How Everything Works:
```csharp
OrderService
   ↓ (OrderPlacedEvent)
RabbitMQ → order-placed queue
   ↓
RiskService
   ↓ (RiskApprovedEvent)
RabbitMQ → risk-approved queue
   ↓
ExecutionService
```

1. To create a container use command:
```bash
 docker-compose up -d
```
2. It will create rabbitMQ image and you can access using port.
3. 