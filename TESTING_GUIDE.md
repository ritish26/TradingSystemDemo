# Testing Guide for Event-Driven Order Processing System

## Overview

This guide provides step-by-step instructions for testing the complete event-driven order processing system with the Order Service and Processing Service.

---

## Prerequisites

- Docker & Docker Compose installed
- .NET 8.0 SDK (for local development)
- Git Bash or similar terminal
- Postman or similar API testing tool (optional)
- RabbitMQ Management Console access

---

## Test Scenario 1: Complete Flow with Docker Compose

### Step 1: Start All Services

```bash
cd /Users/ritikdhiman/Desktop/Interview-preapration/TradingSystemDemo
docker-compose up -d
```

**Verify all services are running:**
```bash
docker ps
```

Expected output:
```
CONTAINER ID   IMAGE                         PORTS
xxxxx          trading-rabbitmq              0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
xxxxx          trading-orderservice          0.0.0.0:5001->80/tcp
xxxxx          trading-processingservice     0.0.0.0:5002->80/tcp
```

### Step 2: Verify RabbitMQ is Ready

Open RabbitMQ Management Console:
```
http://localhost:15672
Username: guest
Password: guest
```

Check:
- ✅ RabbitMQ is running
- ✅ 3 connections visible (two services + management)

### Step 3: Check Service Health

#### Order Service Health
```bash
curl http://localhost:5001/api/health/status
```

Response:
```json
{
  "service": "OrderService",
  "status": "Healthy",
  "timestamp": "2024-04-14T10:30:00Z"
}
```

#### Processing Service Health
```bash
curl http://localhost:5002/api/health/status
```

Response:
```json
{
  "service": "ProcessingService",
  "status": "Healthy",
  "timestamp": "2024-04-14T10:30:00Z",
  "message": "Processing Service is running and listening for OrderPlaced events"
}
```

### Step 4: Place an Order (Test Happy Path)

```bash
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "CLIENT-001",
    "instrumentId": "AAPL",
    "quantity": 100,
    "orderType": "BUY",
    "price": 150.50
  }'
```

**Expected Response:**
```json
{
  "orderId": "ORDER-12345",
  "status": "Submitted",
  "message": "Order submitted for processing"
}
```

### Step 5: Monitor Processing Service Logs

```bash
docker logs trading-processingservice -f
```

**Expected Log Sequence:**
```
info: ProcessingService.BackgroundService.RabbitConsumerService
      Message received from queue: {"orderId":"ORDER-12345",...}

info: ProcessingService.Consumers.OrderPlacedConsumer
      Processing OrderPlacedEvent for Order ORDER-12345

info: ProcessingService.Service.OrderValidator
      Validating order ORDER-12345

info: ProcessingService.Service.OrderValidator
      Order ORDER-12345 passed validation

info: ProcessingService.Service.OrderExecutor
      Executing order ORDER-12345 for instrument AAPL

info: ProcessingService.Service.OrderExecutor
      Order ORDER-12345 executed successfully. ExecutionId: EXEC-xxxxx
```

### Step 6: Verify in RabbitMQ Management

1. Navigate to `http://localhost:15672/`
2. Go to **Queues** tab
3. Check `order-placed-events` queue
4. Should show: **Total messages: 0** (consumed and processed)

---

## Test Scenario 2: Validation Failure

### Test Invalid Order

```bash
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "CLIENT-002",
    "instrumentId": "AAPL",
    "quantity": -100,
    "orderType": "BUY",
    "price": 150.50
  }'
```

**Processing Service Logs:**
```
warn: ProcessingService.Consumers.OrderPlacedConsumer
      Order validation failed for ORDER-xxxxx: Quantity must be greater than 0
```

---

## Test Scenario 3: Order Size Limit Exceeded

### Test Large Order

```bash
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "CLIENT-003",
    "instrumentId": "AAPL",
    "quantity": 200000,
    "orderType": "BUY",
    "price": 150.50
  }'
```

**Processing Service Logs:**
```
warn: ProcessingService.Consumers.OrderPlacedConsumer
      Order validation failed for ORDER-xxxxx: Order quantity exceeds maximum limit of 100000
```

---

## Test Scenario 4: Order Value Limit Exceeded

### Test High-Value Order

```bash
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "CLIENT-004",
    "instrumentId": "AAPL",
    "quantity": 15000,
    "orderType": "BUY",
    "price": 100.00
  }'
```

**Note:** This order value = 15,000 × 100 = 1,500,000 (exceeds max of 1,000,000)

**Processing Service Logs:**
```
warn: ProcessingService.Consumers.OrderPlacedConsumer
      Order validation failed for ORDER-xxxxx: Order value exceeds maximum limit of 1000000
```

---

## Test Scenario 5: Multiple Concurrent Orders

### Stress Test with Concurrent Orders

```bash
for i in {1..5}; do
  curl -X POST http://localhost:5001/api/orders \
    -H "Content-Type: application/json" \
    -d "{
      \"clientId\": \"CLIENT-STRESS-$i\",
      \"instrumentId\": \"AAPL\",
      \"quantity\": $((RANDOM % 1000 + 1)),
      \"orderType\": \"$([ $((RANDOM % 2)) == 0 ] && echo 'BUY' || echo 'SELL')\",
      \"price\": $(echo "scale=2; 100 + $RANDOM/327" | bc)
    }" &
done
wait
```

**Expected:** All 5 orders processed successfully

**Check Processing Service Logs:**
```bash
docker logs trading-processingservice | grep "executed successfully"
# Should show 5 lines
```

---

## Test Scenario 6: Local Development Testing

### Terminal 1: Start RabbitMQ

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

### Terminal 2: Start Order Service

```bash
cd /Users/ritikdhiman/Desktop/Interview-preapration/TradingSystemDemo/OrderService
dotnet run
```

Expected output:
```
info: OrderService2.Program
      Now listening on: http://localhost:5000
```

### Terminal 3: Start Processing Service

```bash
cd /Users/ritikdhiman/Desktop/Interview-preapration/TradingSystemDemo/ProcessingService
dotnet run
```

Expected output:
```
info: ProcessingService.BackgroundService.RabbitConsumerService
      RabbitConsumerService started and waiting for messages
```

### Terminal 4: Test API

```bash
# Health checks
curl http://localhost:5001/api/health/status
curl http://localhost:5002/api/health/status

# Place order
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "LOCAL-TEST",
    "instrumentId": "MSFT",
    "quantity": 50,
    "orderType": "BUY",
    "price": 320.00
  }'
```

---

## Manual Testing via RabbitMQ Management

### Send Test Event Directly

1. Go to `http://localhost:15672/`
2. Navigate to **Queues** → **order-placed-events**
3. Click **Publish message**
4. Enter Payload:

```json
{
  "orderId": "MANUAL-TEST-001",
  "clientId": "TEST-CLIENT",
  "instrumentSymbol": "GOOGL",
  "orderType": "BUY",
  "quantity": 50,
  "price": 140.00,
  "status": "PLACED",
  "createdAt": "2024-04-14T10:30:00Z"
}
```

5. Click **Publish message**
6. Check Processing Service logs for consumption

---

## Validation Rules Test Matrix

| Test Case | Input | Expected Result |
|-----------|-------|-----------------|
| Valid BUY order | quantity=100, price=150.50 | ✅ Executed |
| Valid SELL order | quantity=100, price=150.50 | ✅ Executed |
| Zero quantity | quantity=0, price=150.50 | ❌ Rejected |
| Negative quantity | quantity=-100, price=150.50 | ❌ Rejected |
| Zero price | quantity=100, price=0 | ❌ Rejected |
| Negative price | quantity=100, price=-150.50 | ❌ Rejected |
| Invalid OrderType | orderType="HOLD" | ❌ Rejected |
| Missing ClientId | clientId=null | ❌ Rejected |
| Missing InstrumentSymbol | symbol=null | ❌ Rejected |
| Quantity exceeds limit | quantity=150000 | ❌ Rejected |
| Value exceeds limit | quantity=20000, price=100 | ❌ Rejected |
| Price > 100000 | price=150000 | ❌ Rejected |

---

## Monitoring Tools

### 1. RabbitMQ Management Console
```
http://localhost:15672
```
- View queues and messages
- Monitor consumers
- Check connection statistics

### 2. Docker Logs

Order Service:
```bash
docker logs trading-orderservice -f
```

Processing Service:
```bash
docker logs trading-processingservice -f
```

RabbitMQ:
```bash
docker logs trading-rabbitmq -f
```

### 3. Service Health Endpoints

```bash
# Order Service
curl http://localhost:5001/api/health/status

# Processing Service
curl http://localhost:5002/api/health/status
```

---

## Troubleshooting Test Failures

### Messages Not Being Consumed

**Check:**
1. Is Processing Service running?
   ```bash
   docker ps | grep processingservice
   ```

2. Is RabbitMQ running?
   ```bash
   docker ps | grep rabbitmq
   ```

3. Check Processing Service logs:
   ```bash
   docker logs trading-processingservice | head -50
   ```

**Solution:**
- Restart services: `docker-compose restart`
- Check RabbitMQ connectivity settings in appsettings.json

### Messages in Queue But Not Consumed

**Check:**
1. Navigate to RabbitMQ Management Console
2. Check if consumer is connected to queue
3. Look for error in Processing Service logs

**Solution:**
- Restart consumer: `docker-compose restart processingservice`
- Check queue name matches (should be: `order-placed-events`)

### Order Service Returns Error

**Check:**
1. Order Service logs: `docker logs trading-orderservice`
2. Validate JSON format
3. Check all required fields are present

**Solution:**
- Verify OrderRequest schema matches API expectations
- Check for typos in field names

---

## Performance Testing

### Load Test with Multiple Orders

```bash
#!/bin/bash
# load_test.sh

echo "Starting load test..."
for i in {1..10}; do
  echo "Sending order $i..."
  curl -s -X POST http://localhost:5001/api/orders \
    -H "Content-Type: application/json" \
    -d "{
      \"clientId\": \"LOAD-TEST-$i\",
      \"instrumentId\": \"AAPL\",
      \"quantity\": $((RANDOM % 500 + 1)),
      \"orderType\": \"BUY\",
      \"price\": $(echo "scale=2; 100 + $RANDOM/657" | bc)
    }" &
done
wait
echo "Load test complete!"
```

Run:
```bash
chmod +x load_test.sh
./load_test.sh
```

---

## Cleanup

### Stop All Services

```bash
docker-compose down
```

### Clean Up Volumes

```bash
docker-compose down -v
```

### Stop Individual Service

```bash
docker stop trading-processingservice
```

---

## Summary

✅ **Happy Path:** Order submitted → validated → executed successfully  
✅ **Validation:** Business rules enforced correctly  
✅ **Concurrent:** Multiple orders processed simultaneously  
✅ **Monitoring:** Clear logs and RabbitMQ visibility  
✅ **Resilience:** Failed messages requeued and retried  

All systems are working as expected when tests pass!

