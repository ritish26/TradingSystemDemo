#!/bin/bash

# Trading System - Docker Startup Script
# This script builds and runs RabbitMQ and OrderService containers

set -e

echo "=================================="
echo "Trading System - Docker Setup"
echo "=================================="

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Docker is not installed. Please install Docker first.${NC}"
    exit 1
fi

# Check if docker-compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}Docker Compose is not installed. Please install Docker Compose first.${NC}"
    exit 1
fi

echo -e "${YELLOW}Step 1: Building Docker images...${NC}"
docker-compose build --no-cache

echo -e "${YELLOW}Step 2: Starting containers...${NC}"
docker-compose up -d

echo -e "${GREEN}✓ RabbitMQ container started${NC}"
echo -e "${GREEN}✓ OrderService container started${NC}"

echo ""
echo "=================================="
echo "Services are running!"
echo "=================================="
echo ""
echo -e "${GREEN}RabbitMQ Management UI:${NC}"
echo "  URL: http://localhost:15672"
echo "  Username: guest"
echo "  Password: guest"
echo ""
echo -e "${GREEN}OrderService API:${NC}"
echo "  Base URL: http://localhost:5001"
echo "  Swagger: http://localhost:5001/swagger"
echo "   pgAdminUI: http://localhost:5050"
echo ""
echo -e "${YELLOW}Useful Commands:${NC}"
echo "  View logs:       docker-compose logs -f orderservice"
echo "  View RabbitMQ:   docker-compose logs -f rabbitmq"
echo "  Stop services:   docker-compose down"
echo "  Restart:         docker-compose restart"
echo ""
echo -e "${YELLOW}Test the service:${NC}"
echo "  curl http://localhost:5001/api/order/health"
echo ""

