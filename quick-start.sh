#!/bin/bash
# ============================================================
# Coffee Commodity Analytics Platform - Quick Start Script
# For Mac/Linux
# ============================================================

set -e

echo ""
echo "========================================"
echo "Coffee Analytics Platform - Quick Start"
echo "========================================"
echo ""

# Check prerequisites
echo "[1/6] Checking prerequisites..."
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "ERROR: Node.js not found. Please install Node.js from https://nodejs.org/"
    exit 1
fi

if ! command -v docker &> /dev/null; then
    echo "ERROR: Docker not found. Please install Docker Desktop from https://www.docker.com/products/docker-desktop/"
    exit 1
fi

echo "✓ Prerequisites OK"
echo ""

# Check .env file
echo "[2/6] Checking environment configuration..."
if [ ! -f .env ]; then
    echo "Creating .env file from .env.example..."
    cp .env.example .env
    echo ""
    echo "IMPORTANT: Please edit .env file and add your:"
    echo "  - TWELVEDATA_API_KEY (get from https://twelvedata.com/)"
    echo "  - JWT_SECRET (generate with: openssl rand -base64 32)"
    echo ""
    read -p "Press Enter after editing .env file..."
else
    echo "✓ .env file exists"
fi
echo ""

# Start Docker services
echo "[3/6] Starting Docker services..."
docker compose up -d postgres redis
echo "✓ Database and Redis started"
echo ""

# Wait for database to be ready
echo "[4/6] Waiting for database to initialize..."
sleep 15
echo "✓ Database ready"
echo ""

# Start backend
echo "[5/6] Starting Backend API..."
docker compose up -d backend
echo "✓ Backend started on http://localhost:5000"
echo ""

# Start frontend
echo "[6/6] Starting Frontend..."
docker compose up -d frontend
echo "✓ Frontend started on http://localhost:3000"
echo ""

# Start nginx
echo "Starting Nginx reverse proxy..."
docker compose up -d nginx
echo "✓ Nginx started on http://localhost"
echo ""

echo ""
echo "========================================"
echo "✓ System startup complete!"
echo "========================================"
echo ""
echo "Access the application:"
echo "  Frontend:    http://localhost"
echo "  Backend:     http://localhost:5000"
echo "  Swagger:     http://localhost:5000/swagger"
echo "  Health:      http://localhost:5000/health"
echo ""
echo "View logs:"
echo "  docker compose logs -f"
echo ""
echo "Stop system:"
echo "  docker compose down"
echo ""
echo "For detailed setup instructions, see ONBOARDING_GUIDE.md"
echo ""
