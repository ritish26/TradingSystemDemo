# Docker Setup & Deployment Guide

## Prerequisites

Before running the microservices, ensure you have installed:

### macOS
```bash
# Using Homebrew
brew install docker
brew install docker-compose
# Or install Docker Desktop: https://www.docker.com/products/docker-desktop
```

### Windows
- Download and install **Docker Desktop for Windows**: https://www.docker.com/products/docker-desktop
- Ensure WSL 2 (Windows Subsystem for Linux 2) is enabled

### Linux
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install docker.io docker-compose
sudo systemctl start docker
sudo systemctl enable docker

# Add user to docker group (optional, to avoid sudo)
sudo usermod -aG docker $USER
```

---

## Quick Start (Recommended)

### Option 1: Using the Startup Script

#### macOS/Linux
```bash
cd /Users/ritikdhiman/Desktop/Interview-preapration/TradingSystemDemo
./start.sh
```

#### Windows (Command Prompt)
```cmd
cd C:\path\to\TradingSystemDemo
start.bat
```

---

### Option 2: Manual Docker Compose Commands

#### Build and Start
```bash
cd /Users/ritikdhiman/Desktop/Interview-preapration/TradingSystemDemo

# Build images (first time)
docker-compose build

# Start all services in background
docker-compose up -d

# View logs
docker-compose logs -f
```

#### Stop Services
```bash
docker-compose down
```

#### Restart Services
```bash
docker-compose restart
```

---

## Service Details

### RabbitMQ Service

**Image:** `rabbitmq:3-management`

**Ports:**
- `5672` - AMQP (message broker)
- `15672` - Management UI (web interface)

**Default Credentials:**
- Username: `guest`
- Password: `guest`

**Access:**
```
Management UI: http://localhost:15672
Username: guest
Password: guest
```

**Features:**
- Health check enabled (every 30 seconds)
- Data persisted in volume: `rabbitmq_data`
- Auto-recovery enabled

---

### OrderService

**Built from:** `OrderService/Dockerfile`

**Port:** `5001`

**Base URL:** `http://localhost:5001`

**Endpoints:**
- `POST /api/order/create` - Create order
- `GET /api/order/health` - Health check
- `GET /swagger` - Swagger documentation

**Environment Variables:**
```
RabbitMq__HostName=rabbitmq
RabbitMq__Port=5672
RabbitMq__UserName=guest
RabbitMq__Password=guest
ASPNETCORE_ENVIRONMENT=Development
```

---

## Docker Architecture

```
┌─────────────────────────────────────────────┐
│         Docker Compose Network              │
│         (trading-network)                   │
│                                             │
│  ┌──────────────────┐  ┌─────────────────┐ │
│  │    RabbitMQ      │  │  OrderService   │ │
│  │   (Port 5672)    │  │  (Port 5001)    │ │
│  │   (UI: 15672)    │  │                 │ │
│  │                  │  │ Depends on:     │ │
│  │ Data Volume:     │  │ RabbitMQ        │ │
│  │ rabbitmq_data    │  │ (healthy)       │ │
│  │                  │  │                 │ │
│  │ Health: Enabled  │  │ Restart: Auto   │ │
│  └──────────────────┘  └─────────────────┘ │
│                                             │
└─────────────────────────────────────────────┘
```

---

## Docker Compose Configuration

### File: docker-compose.yml

**Key Features:**
- Version: 3.8
- Network: `trading-network` (bridge)
- Volume: `rabbitmq_data` (for persistence)
- Service Dependencies: OrderService waits for RabbitMQ health
- Auto-restart: `unless-stopped`

**Build Context:**
- Context: Root directory (.)
- Dockerfile: OrderService/Dockerfile

---

## Testing the Services

### 1. Check RabbitMQ Health

**Option A: Using Browser**
```
Open: http://localhost:15672
Login with: guest/guest
```

**Option B: Using curl**
```bash
curl -i http://localhost:15672/api/overview
```

### 2. Check OrderService Health

```bash
curl http://localhost:5001/api/order/health
```

**Expected Response:**
```json
{
  "status": "Order Service is healthy"
}
```

### 3. Create an Order

```bash
curl -X POST http://localhost:5001/api/order/create \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "CLIENT-001",
    "instrumentSymbol": "AAPL",
    "orderType": "BUY",
    "quantity": 100,
    "price": 150.50
  }'
```

**Expected Response (202 Accepted):**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "PENDING",
  "message": "Order command published for processing"
}
```

### 4. Monitor Logs

```bash
# All services
docker-compose logs -f

# OrderService only
docker-compose logs -f orderservice

# RabbitMQ only
docker-compose logs -f rabbitmq

# Last 100 lines
docker-compose logs --tail=100 orderservice
```

---

## Useful Docker Commands

### Container Management

```bash
# List running containers
docker-compose ps

# View container details
docker inspect orderservice

# Execute command in container
docker-compose exec orderservice dotnet --version

# Stop all services
docker-compose stop

# Remove all containers
docker-compose down

# Remove containers and volumes
docker-compose down -v

# Rebuild and restart
docker-compose down && docker-compose build && docker-compose up -d
```

### Debugging

```bash
# Shell access to OrderService
docker-compose exec orderservice /bin/bash

# View container file system
docker-compose exec orderservice ls -la /app

# Check RabbitMQ status
docker-compose exec rabbitmq rabbitmq-diagnostics status

# View RabbitMQ queues
docker-compose exec rabbitmq rabbitmqctl list_queues
```

---

## Common Issues & Solutions

### Issue 1: "Cannot connect to RabbitMQ"

**Cause:** RabbitMQ container not healthy yet

**Solution:**
```bash
# Wait a few seconds and retry
# OR check logs
docker-compose logs rabbitmq

# Verify health
docker-compose ps rabbitmq
```

### Issue 2: "Port 5001 already in use"

**Solution A:** Change port in docker-compose.yml
```yaml
ports:
  - "5002:80"  # Use 5002 instead
```

**Solution B:** Stop the service using the port
```bash
lsof -i :5001  # Find process
kill -9 <PID>  # Kill process
```

### Issue 3: "Docker daemon not running"

**macOS:**
```bash
# Start Docker Desktop or use:
open /Applications/Docker.app
```

**Linux:**
```bash
sudo systemctl start docker
```

### Issue 4: "Permission denied while trying to connect to Docker daemon"

**Solution (Linux):**
```bash
sudo usermod -aG docker $USER
newgrp docker
```

---

## Scaling OrderService

To run multiple instances of OrderService:

### Option A: Scale via Docker Compose
```bash
docker-compose up -d --scale orderservice=3
```

**Note:** You'll need to update ports in docker-compose.yml:
```yaml
orderservice:
  # ... other config ...
  ports:
    - "5001-5003:80"  # Maps 5001, 5002, 5003 to container port 80
```

### Option B: Manual Configuration
Create `docker-compose.override.yml`:
```yaml
version: '3.8'

services:
  orderservice1:
    extends:
      service: orderservice
    container_name: orderservice1
    ports:
      - "5001:80"

  orderservice2:
    extends:
      service: orderservice
    container_name: orderservice2
    ports:
      - "5002:80"

  orderservice3:
    extends:
      service: orderservice
    container_name: orderservice3
    ports:
      - "5003:80"
```

---

## Production Considerations

### Security
- [ ] Change RabbitMQ default credentials
- [ ] Enable RabbitMQ SSL/TLS
- [ ] Add password complexity requirements
- [ ] Use secrets management (Docker Secrets, Kubernetes Secrets)

### Performance
- [ ] Increase RabbitMQ memory limits
- [ ] Enable persistent logging
- [ ] Configure message TTL
- [ ] Implement dead-letter queues

### Monitoring
- [ ] Enable Docker stats
- [ ] Setup centralized logging (ELK, Splunk)
- [ ] Monitor RabbitMQ metrics
- [ ] Setup alerting

### High Availability
- [ ] Use RabbitMQ clustering
- [ ] Multiple OrderService replicas
- [ ] Load balancer (Nginx, HAProxy)
- [ ] Backup and recovery procedures

---

## Docker Compose Reference

### docker-compose.yml Structure

```yaml
version: '3.8'              # Compose file format version

services:                   # Define microservices
  rabbitmq:
    image: ...
    container_name: ...
    ports:
      - "external:internal"
    environment:
      - VAR=value
    healthcheck:
      test: [...]
      interval: 30s
      timeout: 10s
      retries: 5
    networks:
      - trading-network
    volumes:
      - volume_name:/path

  orderservice:
    build:
      context: .
      dockerfile: OrderService/Dockerfile
    depends_on:
      rabbitmq:
        condition: service_healthy
    restart: unless-stopped

networks:                   # Define custom networks
  trading-network:
    driver: bridge

volumes:                    # Define persistent volumes
  rabbitmq_data:
```

---

## Files Reference

```
TradingSystemDemo/
├── docker-compose.yml      # Docker Compose configuration
├── start.sh               # Startup script (macOS/Linux)
├── start.bat              # Startup script (Windows)
├── OrderService/
│   ├── Dockerfile         # Docker build instructions
│   ├── Program.cs
│   ├── OrderService.csproj
│   └── ... (other files)
└── DOCKER_GUIDE.md        # This file
```

---

## Next Steps

1. **Start the services** using the startup script or docker-compose
2. **Verify** both services are running with health checks
3. **Test** the OrderService API endpoints
4. **Monitor** logs and RabbitMQ queues
5. **Integrate** RiskService and ExecutionService

---

## Support

For Docker issues, check:
- https://docs.docker.com/compose/
- https://www.rabbitmq.com/
- https://docs.microsoft.com/en-us/dotnet/architecture/microservices/

