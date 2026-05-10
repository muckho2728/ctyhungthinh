@echo off
REM ============================================================
REM Coffee Commodity Analytics Platform - Quick Start Script
REM For Windows
REM ============================================================

echo.
echo ========================================
echo Coffee Analytics Platform - Quick Start
echo ========================================
echo.

REM Check prerequisites
echo [1/6] Checking prerequisites...
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

where node >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Node.js not found. Please install Node.js from https://nodejs.org/
    pause
    exit /b 1
)

where docker >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Docker not found. Please install Docker Desktop from https://www.docker.com/products/docker-desktop/
    pause
    exit /b 1
)

echo ✓ Prerequisites OK
echo.

REM Check .env file
echo [2/6] Checking environment configuration...
if not exist .env (
    echo Creating .env file from .env.example...
    copy .env.example .env
    echo.
    echo IMPORTANT: Please edit .env file and add your:
    echo   - TWELVEDATA_API_KEY (get from https://twelvedata.com/)
    echo   - JWT_SECRET (generate with: openssl rand -base64 32)
    echo.
    pause
) else (
    echo ✓ .env file exists
)
echo.

REM Start Docker services
echo [3/6] Starting Docker services...
docker compose up -d postgres redis
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to start Docker services
    pause
    exit /b 1
)
echo ✓ Database and Redis started
echo.

REM Wait for database to be ready
echo [4/6] Waiting for database to initialize...
timeout /t 15 /nobreak >nul
echo ✓ Database ready
echo.

REM Start backend
echo [5/6] Starting Backend API...
docker compose up -d backend
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to start backend
    pause
    exit /b 1
)
echo ✓ Backend started on http://localhost:5000
echo.

REM Start frontend
echo [6/6] Starting Frontend...
docker compose up -d frontend
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to start frontend
    pause
    exit /b 1
)
echo ✓ Frontend started on http://localhost:3000
echo.

REM Start nginx
echo Starting Nginx reverse proxy...
docker compose up -d nginx
echo ✓ Nginx started on http://localhost
echo.

echo.
echo ========================================
echo ✓ System startup complete!
echo ========================================
echo.
echo Access the application:
echo   Frontend:    http://localhost
echo   Backend:     http://localhost:5000
echo   Swagger:     http://localhost:5000/swagger
echo   Health:      http://localhost:5000/health
echo.
echo View logs:
echo   docker compose logs -f
echo.
echo Stop system:
echo   docker compose down
echo.
echo For detailed setup instructions, see ONBOARDING_GUIDE.md
echo.
pause
