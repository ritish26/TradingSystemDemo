@echo off
REM Trading System - Docker Startup Script for Windows

echo ==================================
echo Trading System - Docker Setup
echo ==================================
echo.

REM Check if docker is installed
where docker >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Docker is not installed. Please install Docker Desktop first.
    exit /b 1
)

REM Check if docker-compose is installed
where docker-compose >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Docker Compose is not installed. Please install Docker Desktop first.
    exit /b 1
)

echo Step 1: Building Docker images...
docker-compose build --no-cache
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to build Docker images
    exit /b 1
)

echo Step 2: Starting containers...
docker-compose up -d
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to start containers
    exit /b 1
)

echo.
echo ==================================
echo Services are running!
echo ==================================
echo.
echo RabbitMQ Management UI:
echo   URL: http://localhost:15672
echo   Username: guest
echo   Password: guest
echo.
echo OrderService API:
echo   Base URL: http://localhost:5001
echo   Swagger: http://localhost:5001/swagger
echo.
echo Useful Commands:
echo   View logs:       docker-compose logs -f orderservice
echo   View RabbitMQ:   docker-compose logs -f rabbitmq
echo   Stop services:   docker-compose down
echo   Restart:         docker-compose restart
echo.
echo Test the service:
echo   curl http://localhost:5001/api/order/health
echo.
pause

