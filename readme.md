# Event-Driven Order Processing System

## Overview

Design and implement an event-driven order processing system for a wealth management platform.

When a client places an investment order (buy/sell of financial instruments), the system processes the trade asynchronously across two independent microservices in a loosely coupled manner.

The **Order Service** captures the trade request and publishes an **OrderPlaced** event to a message broker (RabbitMQ). The **Processing Service** consumes this event, validates and processes the trade against business logic, and executes the trade.

The system is designed to be loosely coupled, scalable, and resilient, ensuring that failures in downstream services do not impact order intake and that all trade events are reliably processed.

## System Flow

```
Client places trade
   ↓
Order Service → OrderPlacedEvent
   ↓
RabbitMQ
   ↓
Processing Service → Validation & Execution
```

## Project Structure

```
TradingSystemDemo/
│
├── TradingSystemDemo.sln
│
├── OrderService/              🟢 (Order Intake & Publishing)
├── ProcessingService/         🔵 (Order Validation & Execution)
│
├── Shared/                    📦 (Common contracts & events)
│
├── docker-compose.yml         🐳 (RabbitMQ orchestration)
│
├── readme.md
└── Implementation-summary.md
```

## Architecture Components

### 1. Order Service - Order Intake Layer

Responsible for accepting trade requests and publishing events to the message queue.

```
OrderService/
│
├── Controllers/
│   └── OrderController.cs           → POST /api/orders
│
├── Requests/
│   ├── Models/
│   │   └── OrderRequest.cs          → API request DTO
│   ├── OrderMappingProfile.cs       → AutoMapper profile
│   └── OrderRequestValidtor.cs      → Fluent validation rules
│
├── Command/
│   ├── OrderCreatedCommand.cs       → Command object
│   └── OrderCreatedCommandHandler.cs → Command handler
│
├── Service/
│   └── OrderPublisher.cs            → Event publisher to RabbitMQ
│
├── Infrastructure/
│   ├── CommandPublisher.cs          → Command execution via queue
│   └── RabbbitMqConnection.cs       → RabbitMQ connection management
│
├── BackgroundServices/
│   └── CommandConsumerService.cs    → Async command processing
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── OrderService.csproj
└── Dockerfile
```

**Workflow:**
1. API Controller receives `OrderRequest`
2. AutoMapper converts request to domain model
3. Fluent Validation validates the request
4. `OrderCreatedCommand` is created
5. Command is sent to command handler via RabbitMQ queue
6. Command handler publishes `OrderPlacedEvent` to the exchange

---

### 2. Processing Service - Order Validation & Execution Layer

Consumes `OrderPlaced` events and processes them (validation and execution).

```
ProcessingService/
│
├── Consumers/
│   └── OrderPlacedConsumer.cs       → Listens to order-placed queue
│
├── Services/
│   ├── OrderValidator.cs            → Business rules validation
│   └── OrderExecutor.cs             → Trade execution logic
│
├── BackgroundServices/
│   └── RabbitConsumerService.cs     → Hosted service for event consumption
│
├── Infrastructure/
│   └── RabbitMqConnection.cs        → RabbitMQ connection management
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── ProcessingService.csproj
└── Dockerfile
```

**Workflow:**
1. Listens on `order-placed` queue
2. Receives `OrderPlacedEvent`
3. Validates order against business rules
4. Executes the trade
5. Logs results to database/storage

---

### 3. Shared Library - Common Contracts

Contains shared models, events, and constants used by both services.

```
Shared/
│
├── Events/
│   ├── OrderPlacedEvent.cs          → Fired by Order Service
│   └── OrderProcessedEvent.cs       → (Optional) Fired by Processing Service
│
├── Constants/
│   └── QueueNames.cs                → Queue name constants
│
├── DTOs/
│   └── OrderDto.cs                  → Shared data transfer objects
│
├── Program.cs
└── Shared.csproj
```

---

## Key Technologies & Patterns

### Microservices Communication
- **Message Broker:** RabbitMQ (asynchronous, event-driven)
- **Pattern:** Event Sourcing with Command Query Responsibility Segregation (CQRS)

### Order Service Patterns
- **Request DTO:** Fluent validation for API inputs
- **AutoMapper:** Maps API requests to domain commands
- **Command Pattern:** `OrderCreatedCommand` with dedicated handler
- **Event Publishing:** Publishes `OrderPlacedEvent` to message queue

### Processing Service Patterns
- **Event Consumer:** Asynchronous event processing
- **Business Logic:** Validation and execution services
- **Resilience:** Hosted background service for reliable consumption

---

## Docker Compose Configuration

```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: trading-rabbitmq
    ports:
      - "5672:5672"        # AMQP port
      - "15672:15672"      # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 10s
      retries: 5

  orderservice:
    build: ./OrderService
    container_name: trading-orderservice
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RabbitMQ__HostName=rabbitmq
      - RabbitMQ__Port=5672
    depends_on:
      rabbitmq:
        condition: service_healthy
    networks:
      - trading-network

  processingservice:
    build: ./ProcessingService
    container_name: trading-processingservice
    ports:
      - "5002:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RabbitMQ__HostName=rabbitmq
      - RabbitMQ__Port=5672
    depends_on:
      rabbitmq:
        condition: service_healthy
    networks:
      - trading-network

networks:
  trading-network:
    driver: bridge
```

---

## Getting Started

### Prerequisites
- Docker & Docker Compose installed
- .NET 8.0 SDK (for local development)
- RabbitMQ (via Docker)

### Running the System

#### Option 1: Docker Compose (Recommended)
```bash
docker-compose up -d
```

This will:
- Start RabbitMQ on `http://localhost:15672` (default: guest/guest)
- Start Order Service on `http://localhost:5001`
- Start Processing Service on `http://localhost:5002`

#### Option 2: Local Development
```bash
# Terminal 1: Order Service
cd OrderService
dotnet run

# Terminal 2: Processing Service
cd ProcessingService
dotnet run

# Terminal 3: Start RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

---

## API Usage

### Place an Order

**Request:**
```http
POST http://localhost:5001/api/orders
Content-Type: application/json

{
  "clientId": "CLIENT-001",
  "instrumentId": "AAPL",
  "quantity": 100,
  "orderType": "BUY",
  "price": 150.50
}
```

**Response:**
```json
{
  "orderId": "ORDER-12345",
  "status": "Submitted",
  "message": "Order submitted for processing"
}
```

---

## Processing Flow (Detailed)

```
1. Client submits OrderRequest via REST API
   ↓
2. Order Service validates request (Fluent Validation)
   ↓
3. OrderRequest → (AutoMapper) → Domain model
   ↓
4. Create OrderCreatedCommand
   ↓
5. Send command to RabbitMQ command queue via CommandPublisher
   ↓
6. CommandConsumerService picks up the command
   ↓
7. OrderCreatedCommandHandler executes and publishes OrderPlacedEvent
   ↓
8. OrderPlacedEvent → RabbitMQ exchange → order-placed queue
   ↓
9. Processing Service consumes OrderPlacedEvent
   ↓
10. OrderValidator validates business rules
    ↓
11. OrderExecutor executes the trade
    ↓
12. Order processing complete ✓
```

---

## Testing the System

### RabbitMQ Management Console
Access at: `http://localhost:15672`
- Username: `guest`
- Password: `guest`

View:
- Active queues: `order-commands`, `order-placed`
- Message rates and consumers
- Dead letter queues for failed messages

---

## Error Handling & Resilience

- **Dead Letter Queues (DLQ):** Failed messages are routed to DLQ for manual review
- **Retry Logic:** Configurable retry attempts with exponential backoff
- **Logging:** Structured logging for debugging and monitoring
- **Idempotency:** Order processing is idempotent to handle duplicate events

---

## Next Steps

1. Implement comprehensive error handling in both services
2. Add correlation IDs for distributed tracing
3. Implement monitoring and alerting (Prometheus, ELK stack)
4. Add persistence layer for order history
5. Implement circuit breakers for resilience
6. Add unit and integration tests

---

## Summary

**Architecture:** 2-Service Event-Driven System  
- **Order Service:** Receives API requests, validates, and publishes events
- **Processing Service:** Consumes events and executes business logic

**Technologies:** .NET 8.0, RabbitMQ, AutoMapper, Fluent Validation, CQRS Pattern 


---- JIRA STORY ---
# Implement Outbox Pattern with PostgreSQL for Reliable Order Event Publishing

| Field      | Value                                                              |
|------------|--------------------------------------------------------------------|
| **Type**   | Story                                                              |
| **Priority** | High                                                             |
| **Labels** | `backend` `microservices` `reliability` `trading-system`          |
| **Epic**   | Order Processing Resilience                                        |

---

## User Story

> As a **trading system operator**,
> I want **order events to be reliably published to RabbitMQ even when the broker is temporarily unavailable**,
> so that **no trade orders are silently lost due to infrastructure failures**.

---

## Background

Currently the Order Service publishes `OrderPlacedEvent` directly to RabbitMQ inside `OrderCreatedCommandHandler`. This creates a dual-write problem — if the service crashes between saving the order and publishing the event, the event is lost with no recovery mechanism. For a wealth management platform this is unacceptable.

The Outbox Pattern resolves this by writing the event to the same database transaction as the order, then having a relay service publish it asynchronously.

### Current (broken) flow

```
OrderCreatedCommandHandler
  → INSERT INTO Orders        ← save succeeds
  → Publish to RabbitMQ      ← crash here = event lost forever
```

### Target flow

```
OrderCreatedCommandHandler
  → BEGIN TRANSACTION
      INSERT INTO Orders
      INSERT INTO OutboxMessages (Status = Pending)
  → COMMIT

OutboxRelayService (background)
  → Poll OutboxMessages WHERE Status = Pending
  → Publish to RabbitMQ
  → UPDATE OutboxMessages SET Status = Processed
  → UPDATE Orders SET Status = OrderPlaced

Processing Service
  → Consume OrderPlacedEvent
  → Validate + Execute trade
  → Publish OrderProcessedEvent

Order Service (new consumer)
  → Consume OrderProcessedEvent
  → UPDATE Orders SET Status = Executed
```

---

## Database Schema

### Orders

| Column         | Type               | Notes                        |
|----------------|--------------------|------------------------------|
| `OrderId`      | uniqueidentifier   | PK                           |
| `ClientId`     | nvarchar(50)       |                              |
| `InstrumentId` | nvarchar(20)       |                              |
| `OrderType`    | nvarchar(10)       | BUY / SELL                   |
| `Quantity`     | int                |                              |
| `Price`        | decimal(18,4)      |                              |
| `CreatedAt`    | datetime2          |                              |
| `Status`       | nvarchar(20)       | See status lifecycle below   |

**Status lifecycle:** `Pending` → `OrderPlaced` → `Executed` / `PublishFailed` / `ExecutionFailed`

### OutboxMessages

| Column         | Type               | Notes                        |
|----------------|--------------------|------------------------------|
| `Id`           | uniqueidentifier   | PK                           |
| `OrderId`      | uniqueidentifier   | FK → Orders.OrderId          |
| `EventType`    | nvarchar(100)      | e.g. `OrderPlacedEvent`      |
| `Payload`      | nvarchar(max)      | JSON-serialized event        |
| `CreatedAt`    | datetime2          |                              |
| `ProcessedAt`  | datetime2?         | Null until published         |
| `RetryCount`   | int                | Max 3 before marking Failed  |
| `Status`       | nvarchar(20)       | Pending / Processed / Failed |

> Both tables live in the same PostgreSQL database and are always written in a single atomic transaction.

---

## Acceptance Criteria

- [ ] PostgreSQL is added to `docker-compose.yml` and both `Orders` and `OutboxMessages` tables are created via EF Core migrations
- [ ] `OrderCreatedCommandHandler` writes to both tables in a **single atomic transaction** — RabbitMQ is not touched at this point
- [ ] `OutboxRelayService` polls `OutboxMessages WHERE Status = Pending`, publishes to RabbitMQ, marks rows `Processed`, and updates `Orders.Status = OrderPlaced`
- [ ] Relay retries up to **3 times with exponential backoff** before marking a row `Failed`
- [ ] Processing Service publishes `OrderProcessedEvent` after successful trade execution
- [ ] Order Service consumes `OrderProcessedEvent` and updates `Orders.Status = Executed`
- [ ] `Processed` outbox rows are **not deleted** — retained for audit trail
- [ ] All services resolve PostgreSQL and RabbitMQ via Docker Compose service names (no hardcoded IPs)
- [ ] Unit tests cover the atomic transaction logic and relay retry behaviour

---

## Tasks

- [ ] Add PostgreSQL service to `docker-compose.yml`
- [ ] Install `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package in Order Service
- [ ] Create `Order` and `OutboxMessage` EF Core entities and `DbContext`
- [ ] Run and verify EF Core migrations
- [ ] Refactor `OrderCreatedCommandHandler` to use atomic transaction (remove direct RabbitMQ publish)
- [ ] Implement `OutboxRelayService` in `BackgroundServices/`
- [ ] Add `OrderProcessedEvent` publishing to `ProcessingService/Services/OrderExecutor.cs`
- [ ] Add `OrderStatusConsumerService` to Order Service to consume `OrderProcessedEvent`
- [ ] Update `docker-compose.yml` with `depends_on: db` in both microservices
- [ ] Write unit tests
- [ ] Update `readme.md` with new architecture and setup steps

---

## Out of Scope

- Archiving old `Processed` outbox rows *(separate story)*
- Prometheus / ELK monitoring *(separate story)*
- Circuit breakers *(separate story)*

---

## Definition of Done

- `docker-compose up -d` starts RabbitMQ, PostgreSQL, Order Service, and Processing Service with no manual steps
- Placing an order via `POST /api/orders` results in a row in both `Orders` and `OutboxMessages`
- Killing RabbitMQ mid-flight and restarting it causes pending outbox messages to be published automatically on recovery
- `Orders.Status` progresses end to end: `Pending` → `OrderPlaced` → `Executed`
