# Coffee Commodity Analytics Platform - Developer Onboarding Guide

**Version**: 1.0  
**Last Updated**: 2026-05-09  
**Target Audience**: Junior/Student Developers, New Team Members

---

## Table of Contents
1. [Prerequisites](#1-prerequisites)
2. [Project Overview](#2-project-overview)
3. [Clone Project](#3-clone-project)
4. [Environment Variables Setup](#4-environment-variables-setup)
5. [Database Setup](#5-database-setup)
6. [Redis Setup](#6-redis-setup)
7. [Backend Setup (.NET 8)](#7-backend-setup-net-8)
8. [Frontend Setup (React + Vite)](#8-frontend-setup-react--vite)
9. [Docker Setup (Recommended)](#9-docker-setup-recommended)
10. [Nginx Setup](#10-nginx-setup)
11. [Full System Startup](#11-full-system-startup)
12. [Testing & Verification](#12-testing--verification)
13. [Debugging & Troubleshooting](#13-debugging--troubleshooting)
14. [Production Deployment](#14-production-deployment)
15. [Common Development Tasks](#15-common-development-tasks)

---

## 1. Prerequisites

### 1.1 Required Software

#### A. Node.js (for Frontend)
**Purpose**: Run React frontend with Vite  
**Required Version**: 18.x or higher (20.x recommended)

**Install**:
- Windows: Download from https://nodejs.org/
- Mac: `brew install node`
- Linux: `sudo apt install nodejs npm`

**Verify**:
```bash
node --version
# Expected output: v20.x.x or higher

npm --version
# Expected output: 10.x.x or higher
```

**Common Issues**:
- **Issue**: `node: command not found`
  - **Fix**: Add Node.js to PATH or restart terminal
- **Issue**: Version too old
  - **Fix**: Install latest LTS version from nodejs.org

---

#### B. .NET SDK 8 (for Backend)
**Purpose**: Run ASP.NET Core backend API  
**Required Version**: 8.0.x

**Install**:
- Windows: Download from https://dotnet.microsoft.com/download/dotnet/8.0
- Mac: `brew install dotnet@8`
- Linux: Follow instructions at https://learn.microsoft.com/en-us/dotnet/core/install/linux

**Verify**:
```bash
dotnet --version
# Expected output: 8.0.x

dotnet --list-sdks
# Expected output: 8.0.x [...]
```

**Common Issues**:
- **Issue**: `dotnet: command not found`
  - **Fix**: Add .NET to PATH or restart terminal
- **Issue**: Multiple SDK versions installed
  - **Fix**: Use `dotnet --version` to check active version, or set global version: `dotnet new globaljson --sdk-version 8.0.x`

---

#### C. Python (for ML Service - Optional)
**Purpose**: Run Python ML Service with FastAPI  
**Required Version**: 3.10 or higher

**Install**:
- Windows: Download from https://python.org/
- Mac: `brew install python@3.11`
- Linux: `sudo apt install python3 python3-pip`

**Verify**:
```bash
python --version
# Expected output: Python 3.10.x or higher

pip --version
# Expected output: pip 23.x.x or higher
```

**Common Issues**:
- **Issue**: `python: command not found`
  - **Fix**: Use `python3` instead, or create alias
- **Issue**: Pip not found
  - **Fix**: `python -m ensurepip --upgrade`

---

#### D. Docker Desktop (Required for Docker Compose)
**Purpose**: Run entire system in containers  
**Required Version**: 4.0 or higher

**Install**:
- Windows/Mac: Download from https://www.docker.com/products/docker-desktop/
- Linux: Install Docker Engine + Docker Compose

**Verify**:
```bash
docker --version
# Expected output: Docker version 24.x.x or higher

docker compose version
# Expected output: Docker Compose version v2.x.x or higher
```

**Common Issues**:
- **Issue**: Docker daemon not running
  - **Fix**: Start Docker Desktop application
- **Issue**: Permission denied
  - **Fix**: Add user to docker group (Linux) or run as administrator (Windows)
- **Issue**: WSL2 not installed (Windows)
  - **Fix**: Enable WSL2 in Windows Features

---

#### E. Git
**Purpose**: Clone and manage source code  
**Required Version**: 2.x or higher

**Install**:
- Windows: Download from https://git-scm.com/
- Mac: `brew install git`
- Linux: `sudo apt install git`

**Verify**:
```bash
git --version
# Expected output: git version 2.x.x
```

---

#### F. PostgreSQL Client (Optional for local development)
**Purpose**: Connect to database directly  
**Required Version**: 14 or higher (compatible with Docker image)

**Install**:
- Windows: Download from https://www.postgresql.org/download/windows/
- Mac: `brew install postgresql@14`
- Linux: `sudo apt install postgresql-client`

**Verify**:
```bash
psql --version
# Expected output: psql (PostgreSQL) 14.x or higher
```

---

#### G. Redis CLI (Optional for local development)
**Purpose**: Connect to Redis directly  
**Required Version**: 7.x or higher

**Install**:
- Windows: Use Docker or WSL
- Mac: `brew install redis`
- Linux: `sudo apt install redis-tools`

**Verify**:
```bash
redis-cli --version
# Expected output: redis-cli 7.x.x
```

---

### 1.2 Optional but Recommended Tools

- **VS Code**: Code editor with extensions
  - Extensions: C#, Python, Docker, ESLint, Prettier
- **Postman/Insomnia**: API testing tool
- **DBeaver/TablePlus**: Database GUI tool
- **GitKraken/SourceTree**: Git GUI tool

---

## 2. Project Overview

### 2.1 Architecture

```
┌─────────────────┐
│   Frontend      │  React + Vite (Port 3000)
│   (React)       │
└────────┬────────┘
         │ HTTP/HTTPS
         ↓
┌─────────────────┐
│   Nginx         │  Reverse Proxy (Port 80)
└────────┬────────┘
         │
         ├──────────────┬──────────────┐
         ↓              ↓              ↓
┌─────────────────┐ ┌─────────────┐ ┌─────────────┐
│   Backend API   │ │  PostgreSQL │ │   Redis     │
│   (.NET 8)      │ │  (Port 5432)│ │  (Port 6379)│
│   (Port 5000)   │ └─────────────┘ └─────────────┘
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ TwelveData API  │  External Data Source
└─────────────────┘
```

### 2.2 Folder Structure

```
Coffee Commodity Analytics Platform/
├── backend/                    # .NET 8 Backend
│   ├── CoffeeAnalytics.API/    # API Layer (Controllers, Middleware)
│   ├── CoffeeAnalytics.Application/  # Application Layer (DTOs, Interfaces)
│   ├── CoffeeAnalytics.Domain/ # Domain Layer (Entities, Interfaces)
│   ├── CoffeeAnalytics.Infrastructure/  # Infrastructure (DB, Redis, External APIs)
│   └── Dockerfile             # Backend Docker image
├── frontend/                   # React Frontend
│   ├── src/
│   │   ├── components/        # React components
│   │   ├── pages/            # Page components
│   │   ├── services/         # API client
│   │   └── main.tsx          # Entry point
│   ├── public/               # Static assets
│   ├── package.json          # Node dependencies
│   ├── vite.config.ts        # Vite configuration
│   └── Dockerfile            # Frontend Docker image
├── database/                  # Database scripts
│   ├── init.sql              # Initial schema
│   └── migrations/           # Database migrations
│       └── 001_add_subscription_tables.sql
├── nginx/                     # Nginx configuration
│   ├── nginx.conf            # Main config
│   └── conf.d/
│       └── default.conf      # Site config
├── docker-compose.yml        # Docker orchestration
├── .env.example              # Environment variables template
└── .gitignore               # Git ignore rules
```

### 2.3 Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Frontend | React, Vite, TypeScript | 19.x, 8.x |
| Backend | ASP.NET Core | 8.0 |
| Database | PostgreSQL | 16 |
| Cache | Redis | 7 |
| Auth | JWT | - |
| API Docs | Swagger/OpenAPI | - |
| Charts | TradingView Lightweight Charts | 5.x |
| State Management | Zustand, React Query | 5.x |
| Styling | TailwindCSS | 4.x |
| Container | Docker, Docker Compose | Latest |

---

## 3. Clone Project

### 3.1 Clone Repository

**Purpose**: Get project source code from Git repository

**Command**:
```bash
# Clone the repository
git clone <repository-url> "Coffee Commodity Analytics Platform"

# Navigate to project directory
cd "Coffee Commodity Analytics Platform"
```

**Explanation**:
- `git clone`: Downloads repository from remote
- `<repository-url>`: Replace with actual Git URL (GitHub/GitLab/Bitbucket)
- `cd`: Changes to project directory

**Expected Result**:
- Project files downloaded to local machine
- You're now in project root directory

**Verify**:
```bash
# List files in current directory
ls  # On Mac/Linux
dir # On Windows PowerShell

# Expected: backend, frontend, database, nginx, docker-compose.yml, etc.
```

**Common Issues**:
- **Issue**: `Permission denied`
  - **Fix**: Check repository permissions or use SSH key instead of HTTPS
- **Issue**: `fatal: destination path already exists`
  - **Fix**: Delete existing folder or clone to different location

---

### 3.2 Check Branch

**Purpose**: Ensure you're on correct branch

**Command**:
```bash
# Check current branch
git branch

# Switch to main branch if needed
git checkout main

# Pull latest changes
git pull origin main
```

**Expected Result**:
- `* main` (or your branch name) shown with asterisk

---

## 4. Environment Variables Setup

### 4.1 Create .env File

**Purpose**: Store sensitive configuration (API keys, passwords, etc.)  
**Why**: Never commit secrets to Git

**Command**:
```bash
# Copy .env.example to .env
copy .env.example .env     # Windows PowerShell
cp .env.example .env       # Mac/Linux

# Edit .env file
notepad .env               # Windows
code .env                  # VS Code
nano .env                  # Mac/Linux terminal
```

**Explanation**:
- `.env.example`: Template file with placeholder values
- `.env`: Actual file with your real values (not committed to Git)

---

### 4.2 Fill in Environment Variables

**Purpose**: Configure all required settings for the application

**Edit `.env` file and replace placeholder values**:

```bash
# ============================================================
# Coffee Commodity Analytics Platform — Environment Variables
# ============================================================

# ─── TwelveData API Key (REQUIRED) ───────────────────────────
# Get free API key from: https://twelvedata.com/
# Purpose: Fetch real-time and historical coffee commodity data
TWELVEDATA_API_KEY=your_actual_twelvedata_api_key_here
TWELVEDATA_BASE_URL=https://api.twelvedata.com

# ─── PostgreSQL Database (Docker uses these) ─────────────────
# Purpose: Store user data, market data, predictions, alerts
# Docker Compose will create database automatically
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_DB=coffee_analytics
POSTGRES_USER=coffee_user
POSTGRES_PASSWORD=StrongPassword123!  # Change for production!

# ─── Redis Cache (Docker uses these) ─────────────────────────
# Purpose: Cache API responses, rate limiting
# Docker Compose will start Redis automatically
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=RedisPassword123!  # Change for production!

# ─── JWT Authentication (REQUIRED) ───────────────────────────
# Purpose: Generate and validate JWT tokens for user authentication
# IMPORTANT: Must be at least 32 characters long!
# Generate random secret: https://www.uuidgenerator.net/api/version4
JWT_SECRET=your_super_secret_jwt_key_at_least_32_characters_long_change_this_now
JWT_ISSUER=CoffeeAnalyticsPlatform
JWT_AUDIENCE=CoffeeAnalyticsUsers
JWT_EXPIRY_MINUTES=60
JWT_REFRESH_EXPIRY_DAYS=7

# ─── Backend Configuration ─────────────────────────────────────
BACKEND_PORT=5000
ASPNETCORE_ENVIRONMENT=Development

# ─── Frontend Configuration ───────────────────────────────────
# Purpose: Frontend API proxy configuration
# For Docker: use /api (nginx handles routing)
# For local dev: use http://localhost:5000
VITE_API_BASE_URL=http://localhost:5000/api
VITE_SIGNALR_URL=http://localhost:5000/hub
FRONTEND_PORT=3000
```

**Variable Explanations**:

| Variable | Purpose | Required |
|----------|---------|----------|
| `TWELVEDATA_API_KEY` | API key for TwelveData (coffee market data) | **YES** |
| `JWT_SECRET` | Secret key for JWT token generation | **YES** (min 32 chars) |
| `POSTGRES_PASSWORD` | PostgreSQL database password | YES (Docker) |
| `REDIS_PASSWORD` | Redis cache password | YES (Docker) |
| `VITE_API_BASE_URL` | Frontend API endpoint | YES |
| `ASPNETCORE_ENVIRONMENT` | Environment mode (Development/Production) | YES |

**How to Get TwelveData API Key**:
1. Go to https://twelvedata.com/
2. Sign up for free account
3. Navigate to API Keys section
4. Copy your API key
5. Paste into `TWELVEDATA_API_KEY`

**How to Generate JWT Secret**:
```bash
# Generate random 32+ character secret
# Option 1: Using online tool
# Visit: https://www.uuidgenerator.net/api/version4

# Option 2: Using Python
python -c "import secrets; print(secrets.token_urlsafe(32))"

# Option 3: Using Node.js
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"

# Option 4: Using OpenSSL (Mac/Linux)
openssl rand -base64 32
```

**Common Issues**:
- **Issue**: JWT secret too short
  - **Error**: `JWT secret must be at least 32 characters long`
  - **Fix**: Generate longer secret using commands above
- **Issue**: TwelveData API key invalid
  - **Error**: `401 Unauthorized` from TwelveData
  - **Fix**: Verify API key is correct and active
- **Issue**: .env file not being read
  - **Fix**: Ensure .env is in project root (same level as docker-compose.yml)

---

## 5. Database Setup

### 5.1 Using Docker Compose (Recommended)

**Purpose**: Let Docker handle database setup automatically  
**Why**: Easiest, consistent across environments

**Command**:
```bash
# Start only database services
docker compose up -d postgres redis

# Verify containers are running
docker compose ps
```

**Explanation**:
- `up`: Start services
- `-d`: Detached mode (run in background)
- `postgres redis`: Only start these services

**Expected Result**:
```
NAME                STATUS    PORTS
coffee_postgres     Up        0.0.0.0:5432->5432/tcp
coffee_redis        Up        0.0.0.0:6379->6379/tcp
```

**Verify Database**:
```bash
# Connect to PostgreSQL
docker exec -it coffee_postgres psql -U coffee_user -d coffee_analytics

# List tables
\dt

# Expected output: users, refresh_tokens, commodity_prices, etc.

# Exit
\q
```

---

### 5.2 Run Database Migration

**Purpose**: Create database schema and initial tables  
**Why**: Database needs structure before application can use it

**Option A: Automatic Migration (Backend handles this)**

The backend automatically runs migrations on startup. When you start the backend, it will:
1. Connect to database
2. Check if migrations are needed
3. Apply migrations automatically

**Option B: Manual Migration (if needed)**

If you need to run migrations manually:

```bash
# Connect to PostgreSQL
docker exec -it coffee_postgres psql -U coffee_user -d coffee_analytics

# Run initial schema
\i /docker-entrypoint-initdb.d/init.sql

# Run subscription tables migration
\i /docker-entrypoint-initdb.d/migrations/001_add_subscription_tables.sql

# Verify tables
\dt

# Expected output:
# Schema |         Name          | Type  |  Owner
# --------+-----------------------+-------+--------------
# public  | alerts                | table | coffee_user
# public  | commodity_prices      | table | coffee_user
# public  | feature_flags         | table | coffee_user
# public  | historical_prices     | table | coffee_user
# public  | predictions           | table | coffee_user
# public  | refresh_tokens        | table | coffee_user
# public  | subscription_plans    | table | coffee_user
# public  | technical_indicators  | table | coffee_user
# public  | usage_tracking        | table | coffee_user
# public  | user_subscriptions    | table | coffee_user
# public  | users                 | table | coffee_user

# Exit
\q
```

**Explanation**:
- `\i`: Execute SQL file
- `init.sql`: Initial database schema (users, prices, predictions, etc.)
- `001_add_subscription_tables.sql`: Subscription and usage tracking tables

---

### 5.3 Verify Database Data

**Purpose**: Ensure database is properly set up with seed data

**Command**:
```bash
# Connect to database
docker exec -it coffee_postgres psql -U coffee_user -d coffee_analytics

# Check subscription plans
SELECT * FROM subscription_plans;

# Expected output: Free and Premium plans with pricing

# Check feature flags
SELECT * FROM feature_flags;

# Expected output: Feature flags for ML predictions, advanced indicators, etc.

# Exit
\q
```

**Common Issues**:
- **Issue**: `database "coffee_analytics" does not exist`
  - **Fix**: Wait for PostgreSQL container to fully start (10-20 seconds)
- **Issue**: `relation "users" does not exist`
  - **Fix**: Run migration scripts manually (see Option B above)
- **Issue**: Permission denied
  - **Fix**: Check database user/password in .env file

---

## 6. Redis Setup

### 6.1 Using Docker Compose (Recommended)

**Purpose**: Start Redis cache automatically  
**Why**: Easiest, consistent with database setup

**Command**:
```bash
# Start Redis (already started with postgres)
docker compose up -d redis

# Verify Redis is running
docker compose ps redis
```

**Expected Result**:
```
NAME           STATUS    PORTS
coffee_redis   Up        0.0.0.0:6379->6379/tcp
```

---

### 6.2 Verify Redis Connection

**Purpose**: Ensure Redis is accessible and working

**Command**:
```bash
# Connect to Redis CLI
docker exec -it coffee_redis redis-cli -a RedisPassword123!

# Test connection
PING

# Expected output: PONG

# Test set/get
SET test "hello"
GET test

# Expected output: "hello"

# Exit
EXIT
```

**Explanation**:
- `redis-cli`: Redis command-line interface
- `-a`: Authenticate with password
- `PING`: Test connection
- `SET/GET`: Store and retrieve data

**Common Issues**:
- **Issue**: `NOAUTH Authentication required`
  - **Fix**: Use `-a RedisPassword123!` to authenticate
- **Issue**: `Connection refused`
  - **Fix**: Check if Redis container is running: `docker compose ps redis`
- **Issue**: Wrong password
  - **Fix**: Check REDIS_PASSWORD in .env file

---

## 7. Backend Setup (.NET 8)

### 7.1 Install Dependencies

**Purpose**: Restore NuGet packages for backend

**Command**:
```bash
# Navigate to backend directory
cd backend

# Restore packages
dotnet restore

# Expected output:
# Restore succeeded.
```

**Explanation**:
- `dotnet restore`: Downloads all NuGet packages specified in .csproj files
- Packages: JWT, Serilog, Entity Framework, Redis client, etc.

**Common Issues**:
- **Issue**: `error NU1102: Unable to find package`
  - **Fix**: Check internet connection, or try `dotnet restore --no-cache`
- **Issue**: Version conflicts
  - **Fix**: Delete `bin` and `obj` folders, then restore again

---

### 7.2 Update appsettings.json for Local Development

**Purpose**: Configure backend to use local database/Redis instead of Docker

**File**: `backend/CoffeeAnalytics.API/appsettings.json`

**For Docker Compose (Recommended)**:
Keep as is - Docker Compose will override these with environment variables from .env

**For Local Development (without Docker)**:
Update connection strings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=coffee_analytics;Username=coffee_user;Password=StrongPassword123!"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=RedisPassword123!"
  },
  "TwelveData": {
    "BaseUrl": "https://api.twelvedata.com",
    "ApiKey": "your_actual_twelvedata_api_key_here"
  },
  "Jwt": {
    "Secret": "your_super_secret_jwt_key_at_least_32_characters_long",
    "Issuer": "CoffeeAnalyticsPlatform",
    "Audience": "CoffeeAnalyticsUsers",
    "ExpiryMinutes": 60,
    "RefreshExpiryDays": 7
  }
}
```

**Important**: Replace placeholder values with actual values from .env file

---

### 7.3 Build Backend

**Purpose**: Compile backend code

**Command**:
```bash
# Build solution
dotnet build

# Expected output:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

**Explanation**:
- `dotnet build`: Compiles all projects in solution
- Checks for syntax errors, missing references

**Common Issues**:
- **Issue**: Build errors
  - **Fix**: Check error messages, usually missing using statements or type mismatches
- **Issue**: Package restore failed
  - **Fix**: Run `dotnet restore` again

---

### 7.4 Run Backend

**Option A: Run with Docker Compose (Recommended)**

```bash
# Navigate to project root
cd ..

# Start backend with Docker
docker compose up -d backend

# View logs
docker compose logs -f backend
```

**Option B: Run Locally (for debugging)**

```bash
# Navigate to API project
cd backend/CoffeeAnalytics.API

# Run API
dotnet run

# Expected output:
# Building...
# Coffee Analytics API starting on Development
# Now listening on: http://localhost:5000
# Application started. Press Ctrl+C to shut down.
```

**Explanation**:
- `dotnet run`: Builds and runs the application
- Backend will start on port 5000
- Database migrations run automatically on startup

---

### 7.5 Access Swagger UI

**Purpose**: Test API endpoints interactively

**URL**: http://localhost:5000/swagger

**What you'll see**:
- Swagger UI with all API endpoints
- Authentication setup (Bearer token)
- Try it out buttons for testing

**Test Authentication**:
1. Click on `/api/auth/register` endpoint
2. Click "Try it out"
3. Fill in request body:
```json
{
  "email": "test@example.com",
  "password": "Test@123456",
  "fullName": "Test User"
}
```
4. Click "Execute"
5. Copy the `accessToken` from response
6. Click "Authorize" button (top right)
7. Paste token with `Bearer ` prefix: `Bearer eyJhbGci...`
8. Now you can test authenticated endpoints

**Common Issues**:
- **Issue**: Swagger not accessible
  - **Fix**: Check if backend is running: `docker compose ps backend`
- **Issue**: 401 Unauthorized
  - **Fix**: Make sure to include "Bearer " prefix before token
- **Issue**: CORS error
  - **Fix**: Check AllowedOrigins in appsettings.json

---

### 7.6 Verify Backend Health

**Purpose**: Ensure backend is running correctly

**Command**:
```bash
# Test health endpoint
curl http://localhost:5000/health

# Expected output:
# {"status":"Healthy"}

# Test metrics endpoint
curl http://localhost:5000/api/metrics

# Expected output: JSON with application metrics
```

---

## 8. Frontend Setup (React + Vite)

### 8.1 Install Dependencies

**Purpose**: Install Node.js packages for frontend

**Command**:
```bash
# Navigate to frontend directory
cd frontend

# Install dependencies
npm install

# Expected output:
# added XXX packages in XXs
```

**Explanation**:
- `npm install`: Downloads all packages from package.json
- Packages: React, TypeScript, TailwindCSS, Axios, etc.

**Common Issues**:
- **Issue**: `EACCES` permission error
  - **Fix**: Use `sudo npm install` (Mac/Linux) or run terminal as administrator (Windows)
- **Issue**: `node_modules` corrupt
  - **Fix**: Delete `node_modules` folder and `package-lock.json`, then run `npm install` again
- **Issue**: Network timeout
  - **Fix**: Check internet connection or use npm mirror: `npm install --registry=https://registry.npmmirror.com`

---

### 8.2 Configure Environment Variables

**Purpose**: Configure frontend to connect to backend API

**File**: `frontend/.env` (create this file if it doesn't exist)

**For Docker Compose**:
```bash
VITE_API_BASE_URL=/api
VITE_SIGNALR_URL=/hub
```

**For Local Development**:
```bash
VITE_API_BASE_URL=http://localhost:5000/api
VITE_SIGNALR_URL=http://localhost:5000/hub
```

**Explanation**:
- `VITE_API_BASE_URL`: Backend API endpoint
- `VITE_SIGNALR_URL`: SignalR WebSocket endpoint for real-time updates
- Vite automatically loads variables starting with `VITE_`

---

### 8.3 Run Frontend

**Option A: Run with Docker Compose (Recommended)**

```bash
# Navigate to project root
cd ..

# Start frontend with Docker
docker compose up -d frontend

# View logs
docker compose logs -f frontend
```

**Option B: Run Locally (for development)**

```bash
# In frontend directory
npm run dev

# Expected output:
#   VITE v8.x.x  ready in XXX ms
# 
#   ➜  Local:   http://localhost:3000/
#   ➜  Network: use --host to expose
```

**Explanation**:
- `npm run dev`: Starts Vite development server
- Frontend runs on port 3000
- Hot module replacement enabled (auto-reload on code changes)

---

### 8.4 Access Frontend

**URL**: http://localhost:3000

**What you'll see**:
- Login page (if not authenticated)
- Dashboard with coffee market data
- Charts, predictions, alerts pages

**Test Login**:
1. Register a new account at `/login` → "Don't have an account? Sign up"
2. Fill in email, password, full name
3. Click "Sign Up"
4. Login with your credentials
5. Navigate through dashboard

---

### 8.5 Verify Frontend Health

**Purpose**: Ensure frontend is running correctly

**Command**:
```bash
# Test frontend health endpoint (if configured)
curl http://localhost:3000/health

# Expected output: healthy

# Or simply open browser
# http://localhost:3000
```

---

### 8.6 Common Frontend Issues

**Issue**: `VITE_API_BASE_URL is not defined`
- **Fix**: Create `.env` file in frontend directory with required variables

**Issue**: CORS error in browser console
- **Fix**: Check backend CORS configuration in Program.cs
- Ensure frontend URL is in AllowedOrigins

**Issue**: Charts not rendering
- **Fix**: Check browser console for errors
- Ensure lightweight-charts package is installed

**Issue**: White screen after build
- **Fix**: Check `vite.config.ts` for correct base path
- For Docker: base path should be `/`

---

## 9. Docker Setup (Recommended)

### 9.1 Overview

**Purpose**: Run entire system in containers  
**Why**: Consistent environment, easy deployment, isolation

**Architecture**:
```
Docker Compose manages:
- PostgreSQL (database)
- Redis (cache)
- Backend API (.NET 8)
- Frontend (React)
- Nginx (reverse proxy)
```

---

### 9.2 Build All Containers

**Purpose**: Build Docker images for all services

**Command**:
```bash
# Navigate to project root
cd "Coffee Commodity Analytics Platform"

# Build all containers
docker compose build

# Expected output:
# [+] Building 5.5s (XX/XX) FINISHED
# => => naming to docker.io/library/coffee_backend
# => => naming to docker.io/library/coffee_frontend
```

**Explanation**:
- `docker compose build`: Builds all services defined in docker-compose.yml
- First build takes longer (downloads base images)
- Subsequent builds are faster (uses cache)

---

### 9.3 Start All Services

**Purpose**: Start entire application stack

**Command**:
```bash
# Start all services in detached mode
docker compose up -d

# View all running containers
docker compose ps

# Expected output:
# NAME                STATUS    PORTS
# coffee_postgres     Up        0.0.0.0:5432->5432/tcp
# coffee_redis        Up        0.0.0.0:6379->6379/tcp
# coffee_backend      Up        0.0.0.0:5000->5000/tcp
# coffee_frontend     Up        0.0.0.0:80->80/tcp
# coffee_nginx        Up        0.0.0.0:80->80/tcp, 0.0.0.0:443->443/tcp
```

**Explanation**:
- `up`: Start services
- `-d`: Detached mode (run in background)
- Services start in dependency order (postgres → redis → backend → frontend → nginx)

---

### 9.4 View Logs

**Purpose**: Monitor service logs for debugging

**Command**:
```bash
# View logs for all services
docker compose logs -f

# View logs for specific service
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f postgres

# Stop logs with Ctrl+C
```

**Explanation**:
- `-f`: Follow logs (stream new logs)
- Useful for debugging startup issues

---

### 9.5 Stop Services

**Purpose**: Stop all running containers

**Command**:
```bash
# Stop all services
docker compose down

# Stop and remove volumes (deletes data)
docker compose down -v

# Expected output:
# [+] Running X/X
# Container coffee_backend    Removed
# Container coffee_frontend   Removed
# ...
```

**Explanation**:
- `down`: Stop and remove containers
- `-v`: Remove volumes (WARNING: deletes database data)
- Without `-v`: Data persists in volumes

---

### 9.6 Restart Services

**Purpose**: Restart services after configuration changes

**Command**:
```bash
# Restart specific service
docker compose restart backend

# Restart all services
docker compose restart

# Rebuild and restart (if Dockerfile changed)
docker compose up -d --build backend
```

---

### 9.7 Common Docker Issues

**Issue**: Port already in use
```
Error: Bind for 0.0.0.0:5432 failed: port is already allocated
```
- **Fix**: Stop other services using the port, or change port in docker-compose.yml

**Issue**: Container keeps restarting
- **Fix**: Check logs: `docker compose logs <service-name>`
- Usually configuration error or missing environment variables

**Issue**: Out of disk space
- **Fix**: Clean up Docker: `docker system prune -a`

**Issue**: Cannot connect to database from backend
- **Fix**: Check if postgres container is running: `docker compose ps postgres`
- Wait for postgres to fully initialize (10-20 seconds)

---

## 10. Nginx Setup

### 10.1 Overview

**Purpose**: Reverse proxy for routing requests  
**Why**: Single entry point, SSL termination, load balancing

**Architecture**:
```
Internet
  ↓
Nginx (Port 80/443)
  ├─→ / → Frontend (React)
  ├─→ /api → Backend API
  └─→ /hub → SignalR WebSocket
```

---

### 10.2 Nginx Configuration Files

**Main Config**: `nginx/nginx.conf`
- Worker processes
- Logging
- Rate limiting zones
- Gzip compression

**Site Config**: `nginx/conf.d/default.conf`
- Frontend routing
- API routing
- WebSocket routing
- Security headers

---

### 10.3 Test Nginx Configuration

**Purpose**: Ensure Nginx config is valid before starting

**Command**:
```bash
# Test nginx config (inside container)
docker exec coffee_nginx nginx -t

# Expected output:
# nginx: the configuration file /etc/nginx/nginx.conf syntax is ok
# nginx: configuration file /etc/nginx/nginx.conf test is successful
```

---

### 10.4 Reload Nginx (After Config Changes)

**Purpose**: Apply configuration changes without downtime

**Command**:
```bash
# Reload nginx
docker exec coffee_nginx nginx -s reload

# Or restart nginx container
docker compose restart nginx
```

---

### 10.5 Access via Nginx

**URL**: http://localhost

**Routing**:
- `http://localhost/` → Frontend
- `http://localhost/api/` → Backend API
- `http://localhost/hub/` → SignalR WebSocket
- `http://localhost/health` → Health check

---

## 11. Full System Startup

### 11.1 Quick Start (Docker Compose - Recommended)

**Purpose**: Start entire system with one command

**Command**:
```bash
# Navigate to project root
cd "Coffee Commodity Analytics Platform"

# Ensure .env file exists and is configured
# (See Section 4 for environment variables setup)

# Start all services
docker compose up -d

# View logs
docker compose logs -f

# Wait for all services to start (30-60 seconds)
```

**Expected Output**:
```
[+] Running 6/6
 ✔ Container coffee_postgres  Started
 ✔ Container coffee_redis     Started
 ✔ Container coffee_backend   Started
 ✔ Container coffee_frontend  Started
 ✔ Container coffee_nginx     Started
```

---

### 11.2 Startup Order (Manual/Local Development)

**Purpose**: Start services in correct dependency order

**Step 1: Start Database**
```bash
docker compose up -d postgres redis
```
**Wait**: 10-20 seconds for database initialization

**Step 2: Run Database Migrations**
- Automatic: Backend handles this on startup
- Manual: See Section 5.2

**Step 3: Start Backend**
```bash
# Docker
docker compose up -d backend

# Or local
cd backend/CoffeeAnalytics.API
dotnet run
```

**Wait**: 5-10 seconds for backend startup

**Step 4: Start Frontend**
```bash
# Docker
docker compose up -d frontend

# Or local
cd frontend
npm run dev
```

**Step 5: Start Nginx (if using Docker)**
```bash
docker compose up -d nginx
```

---

### 11.3 Verification Checklist

**Purpose**: Ensure all services are running correctly

```bash
# 1. Check all containers are running
docker compose ps
# Expected: All services show "Up" status

# 2. Check backend health
curl http://localhost:5000/health
# Expected: {"status":"Healthy"}

# 3. Check frontend
curl http://localhost:3000
# Expected: HTML response (or redirect to login)

# 4. Check database connection
docker exec -it coffee_postgres psql -U coffee_user -d coffee_analytics -c "SELECT 1;"
# Expected: ?column?
#             ----------
#                    1

# 5. Check Redis connection
docker exec -it coffee_redis redis-cli -a RedisPassword123! PING
# Expected: PONG

# 6. Test API endpoint
curl http://localhost:5000/api/market/realtime?symbol=KC1
# Expected: JSON with coffee price data
```

---

### 11.4 Access Application

**Frontend**: http://localhost (via Nginx) or http://localhost:3000 (direct)  
**Backend API**: http://localhost:5000/api  
**Swagger Docs**: http://localhost:5000/swagger  
**Health Check**: http://localhost:5000/health

---

## 12. Testing & Verification

### 12.1 Test Authentication Flow

**Purpose**: Verify user registration and login works

**Steps**:

1. **Register User**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456",
    "fullName": "Test User"
  }'

# Expected: 201 Created with accessToken and refreshToken
```

2. **Login**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'

# Expected: 200 OK with accessToken and refreshToken
```

3. **Access Protected Endpoint**
```bash
# Replace YOUR_TOKEN with actual token from login response
curl http://localhost:5000/api/alerts \
  -H "Authorization: Bearer YOUR_TOKEN"

# Expected: 200 OK with user's alerts
```

---

### 12.2 Test Market Data API

**Purpose**: Verify TwelveData integration works

**Steps**:

1. **Get Real-time Price**
```bash
curl http://localhost:5000/api/market/realtime?symbol=KC1

# Expected: JSON with current coffee price
```

2. **Get Chart Data**
```bash
curl http://localhost:5000/api/market/chart?symbol=KC1&interval=1day&outputSize=100

# Expected: JSON with OHLCV data for charting
```

3. **Get Technical Indicators**
```bash
curl http://localhost:5000/api/market/indicators?symbol=KC1&interval=1day

# Expected: JSON with RSI, MACD, SMA, etc.
```

---

### 12.3 Test Frontend

**Purpose**: Verify frontend works end-to-end

**Steps**:

1. Open browser: http://localhost:3000
2. Register new account
3. Login
4. Navigate to Dashboard
5. Verify:
   - Coffee price displays
   - Chart renders
   - No console errors (F12 → Console)

---

### 12.4 Test SignalR WebSocket

**Purpose**: Verify real-time updates work

**Steps**:

1. Open browser DevTools (F12)
2. Go to Network tab
3. Filter by WS (WebSocket)
4. Navigate to application
5. Look for WebSocket connection to `/hub/market`
6. Expected: Connection established with status 101

---

## 13. Debugging & Troubleshooting

### 13.1 Common Issues and Solutions

#### Issue 1: Database Connection Failed

**Symptoms**:
```
Npgsql.PostgresException: Connection refused
```

**Causes**:
- PostgreSQL not running
- Wrong connection string
- Firewall blocking connection

**Solutions**:
```bash
# Check if postgres container is running
docker compose ps postgres

# Check postgres logs
docker compose logs postgres

# Test connection manually
docker exec -it coffee_postgres psql -U coffee_user -d coffee_analytics

# Check connection string in appsettings.json or .env
# Ensure host, port, username, password are correct
```

---

#### Issue 2: Redis Connection Failed

**Symptoms**:
```
StackExchange.Redis.RedisConnectionException: It was not possible to connect
```

**Causes**:
- Redis not running
- Wrong password
- Port blocked

**Solutions**:
```bash
# Check if redis container is running
docker compose ps redis

# Check redis logs
docker compose logs redis

# Test connection manually
docker exec -it coffee_redis redis-cli -a RedisPassword123! PING

# Check Redis password in .env and appsettings.json
```

---

#### Issue 3: JWT Token Invalid

**Symptoms**:
```
401 Unauthorized
Invalid token
```

**Causes**:
- Token expired
- Wrong JWT secret
- Token format incorrect

**Solutions**:
```bash
# Check JWT_SECRET in .env and appsettings.json
# Ensure it's at least 32 characters

# Generate new token by logging in again
# Check token expiration time (default 60 minutes)

# Verify token format in Authorization header
# Correct: Authorization: Bearer eyJhbGci...
# Wrong: Authorization: eyJhbGci... (missing "Bearer " prefix)
```

---

#### Issue 4: CORS Error

**Symptoms** (Browser Console):
```
Access to XMLHttpRequest at 'http://localhost:5000/api/...' 
from origin 'http://localhost:3000' has been blocked by CORS policy
```

**Causes**:
- Frontend URL not in AllowedOrigins
- CORS policy misconfigured

**Solutions**:
```bash
# Check Program.cs in backend
# Ensure frontend URL is in AllowedOrigins

# Example in Program.cs:
services.AddCors(opt => opt.AddPolicy("FrontendPolicy", policy =>
{
    policy.WithOrigins(
        "http://localhost:3000",
        "http://localhost:80"  // If using nginx
    )
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();
}));
```

---

#### Issue 5: TwelveData API Error

**Symptoms**:
```
401 Unauthorized from TwelveData
API key invalid
```

**Causes**:
- Invalid API key
- API key expired
- Rate limit exceeded

**Solutions**:
```bash
# Verify TWELVEDATA_API_KEY in .env
# Get new API key from https://twelvedata.com/

# Check TwelveData status page
# https://status.twelvedata.com/

# Test API key manually
curl "https://api.twelvedata.com/price?symbol=KC1&apikey=YOUR_API_KEY"
```

---

#### Issue 6: Docker Port Conflict

**Symptoms**:
```
Bind for 0.0.0.0:5432 failed: port is already allocated
```

**Causes**:
- Port already in use by another application
- Previous Docker containers still running

**Solutions**:
```bash
# Check what's using the port
# Windows: netstat -ano | findstr :5432
# Mac/Linux: lsof -i :5432

# Stop other service using the port, or

# Change port in docker-compose.yml
# Example:
ports:
  - "5433:5432"  # Use 5433 instead of 5432

# Or stop all Docker containers
docker compose down
docker compose up -d
```

---

#### Issue 7: Frontend Build Failed

**Symptoms**:
```
npm ERR! code ELIFECYCLE
npm ERR! errno 1
```

**Causes**:
- Node modules corrupt
- Dependency conflicts
- TypeScript errors

**Solutions**:
```bash
# Clear cache and reinstall
cd frontend
rm -rf node_modules package-lock.json
npm install

# Check for TypeScript errors
npm run build

# Check specific file for errors
# See error output for file name and line number
```

---

#### Issue 8: Backend Build Failed

**Symptoms**:
```
error CS0246: The type or namespace name 'X' could not be found
```

**Causes**:
- Missing using statement
- Package not restored
- Type mismatch

**Solutions**:
```bash
# Restore packages
cd backend
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build

# Check error message for missing type
# Add appropriate using statement
```

---

#### Issue 9: Container Keeps Restarting

**Symptoms**:
```
STATUS: Restarting (1) X seconds ago
```

**Causes**:
- Configuration error
- Missing environment variables
- Application crash on startup

**Solutions**:
```bash
# Check container logs
docker compose logs <service-name>

# Check environment variables
docker compose config

# Verify .env file is correctly formatted
# No extra spaces, correct variable names
```

---

#### Issue 10: Nginx 502 Bad Gateway

**Symptoms**:
```
502 Bad Gateway
nginx/1.x.x
```

**Causes**:
- Backend not running
- Wrong backend address in nginx config
- Backend rejecting connection

**Solutions**:
```bash
# Check if backend is running
docker compose ps backend

# Check backend logs
docker compose logs backend

# Test backend directly
curl http://localhost:5000/health

# Check nginx config
docker exec coffee_nginx cat /etc/nginx/conf.d/default.conf

# Reload nginx after fix
docker compose restart nginx
```

---

### 13.2 Debugging Commands

**View Container Logs**:
```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f backend
docker compose logs -f postgres
docker compose logs -f redis
```

**Enter Container Shell**:
```bash
# Backend
docker exec -it coffee_backend sh

# PostgreSQL
docker exec -it coffee_postgres sh

# Redis
docker exec -it coffee_redis sh
```

**Check Container Resources**:
```bash
# Container stats (CPU, memory)
docker stats

# Specific container
docker stats coffee_backend
```

**Check Network**:
```bash
# List Docker networks
docker network ls

# Inspect network
docker network inspect coffee_net
```

---

### 13.3 Database Debugging

**Connect to Database**:
```bash
docker exec -it coffee_postgres psql -U coffee_user -d coffee_analytics
```

**Useful SQL Commands**:
```sql
-- List tables
\dt

-- Describe table
\d users

-- Select all users
SELECT * FROM users;

-- Check recent errors
SELECT * FROM commodity_prices ORDER BY timestamp DESC LIMIT 10;

-- Count records
SELECT COUNT(*) FROM users;
SELECT COUNT(*) FROM commodity_prices;
```

---

## 14. Production Deployment

### 14.1 Preparation Checklist

**Before Production**:
- [ ] Change all passwords (JWT_SECRET, POSTGRES_PASSWORD, REDIS_PASSWORD)
- [ ] Set ASPNETCORE_ENVIRONMENT=Production
- [ ] Use strong JWT secret (generate with openssl/rand)
- [ ] Enable HTTPS/SSL
- [ ] Configure domain name
- [ ] Set up backups for database
- [ ] Configure logging aggregation
- [ ] Set up monitoring and alerts
- [ ] Update vulnerable packages (AutoMapper, Microsoft.Extensions.Caching.Memory)

---

### 14.2 Production Environment Variables

**Create `.env.production`**:

```bash
# ─── TwelveData ───────────────────────────────────────────
TWELVEDATA_API_KEY=your_production_api_key
TWELVEDATA_BASE_URL=https://api.twelvedata.com

# ─── PostgreSQL ───────────────────────────────────────────
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_DB=coffee_analytics
POSTGRES_USER=coffee_user
POSTGRES_PASSWORD=CHANGE_THIS_STRONG_PASSWORD_IN_PRODUCTION

# ─── Redis ────────────────────────────────────────────────
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=CHANGE_THIS_STRONG_PASSWORD_IN_PRODUCTION

# ─── JWT ──────────────────────────────────────────────────
# Generate with: openssl rand -base64 32
JWT_SECRET=CHANGE_THIS_TO_RANDOM_32_CHAR_STRING
JWT_ISSUER=CoffeeAnalyticsPlatform
JWT_AUDIENCE=CoffeeAnalyticsUsers
JWT_EXPIRY_MINUTES=60
JWT_REFRESH_EXPIRY_DAYS=7

# ─── Backend ──────────────────────────────────────────────
BACKEND_PORT=5000
ASPNETCORE_ENVIRONMENT=Production

# ─── Frontend ─────────────────────────────────────────────
VITE_API_BASE_URL=/api
VITE_SIGNALR_URL=/hub
FRONTEND_PORT=3000
```

---

### 14.3 Production Docker Compose

**Use production environment file**:

```bash
# Start with production environment
docker compose --env-file .env.production up -d

# Or rename .env.production to .env for production server
```

---

### 14.4 HTTPS Setup with Nginx

**Obtain SSL Certificate**:

**Option A: Let's Encrypt (Free)**

```bash
# Install certbot on host machine
# Ubuntu/Debian:
sudo apt install certbot python3-certbot-nginx

# Generate certificate
sudo certbot --nginx -d yourdomain.com

# Certificates will be automatically configured in nginx
```

**Option B: Self-Signed (Development)**

```bash
# Generate self-signed certificate
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/ssl/key.pem \
  -out nginx/ssl/cert.pem

# Update nginx/conf.d/default.conf:
# Add SSL configuration
```

**Update Nginx Config for HTTPS**:

Edit `nginx/conf.d/default.conf`:

```nginx
server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;

    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # ... rest of config
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}
```

---

### 14.5 Domain Setup

**DNS Configuration**:

1. Buy domain from registrar (Namecheap, GoDaddy, etc.)
2. Add A record pointing to your server IP:
   ```
   Type: A
   Name: @
   Value: YOUR_SERVER_IP
   TTL: 300
   ```
3. Wait for DNS propagation (5-60 minutes)
4. Test: `ping yourdomain.com`

---

### 14.6 Backup Strategy

**Database Backup**:

```bash
# Backup database
docker exec coffee_postgres pg_dump -U coffee_user coffee_analytics > backup.sql

# Restore database
docker exec -i coffee_postgres psql -U coffee_user coffee_analytics < backup.sql

# Automated backup (cron job)
# Add to crontab:
0 2 * * * docker exec coffee_postgres pg_dump -U coffee_user coffee_analytics > /backups/coffee_$(date +\%Y\%m\%d).sql
```

**Volume Backup**:

```bash
# Backup Docker volumes
docker run --rm -v coffee_postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres_backup.tar.gz /data

# Restore volume
docker run --rm -v coffee_postgres_data:/data -v $(pwd):/backup alpine tar xzf /backup/postgres_backup.tar.gz -C /
```

---

### 14.7 Monitoring Setup

**Basic Monitoring**:

```bash
# Container health
docker compose ps

# Resource usage
docker stats

# Logs
docker compose logs -f
```

**Advanced Monitoring** (Recommended):
- **Prometheus**: Metrics collection
- **Grafana**: Visualization dashboards
- **Loki**: Log aggregation
- **Alertmanager**: Alerting

---

## 15. Common Development Tasks

### 15.1 Add New API Endpoint

**Steps**:

1. Create DTO in `backend/CoffeeAnalytics.Application/DTOs/`
2. Create interface in `backend/CoffeeAnalytics.Application/Interfaces/`
3. Implement service in `backend/CoffeeAnalytics.Infrastructure/Services/`
4. Create controller in `backend/CoffeeAnalytics.API/Controllers/`
5. Test with Swagger

---

### 15.2 Add New Frontend Page

**Steps**:

1. Create page component in `frontend/src/pages/`
2. Add route in `frontend/src/App.tsx`
3. Create API call in `frontend/src/services/apiClient.ts`
4. Add navigation link in Sidebar
5. Test in browser

---

### 15.3 Run Database Migration

**Steps**:

1. Create migration SQL file in `database/migrations/`
2. Name it: `XXX_description.sql` (e.g., `002_add_user_preferences.sql`)
3. Run migration:
```bash
docker exec -i coffee_postgres psql -U coffee_user -d coffee_analytics < database/migrations/002_add_user_preferences.sql
```

---

### 15.4 Update Dependencies

**Backend**:
```bash
cd backend
dotnet list package --outdated
dotnet add package <PackageName>
```

**Frontend**:
```bash
cd frontend
npm outdated
npm install <PackageName>@latest
```

---

### 15.5 Clear Docker Cache

```bash
# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune

# Remove everything (WARNING: deletes all data)
docker system prune -a --volumes
```

---

## 16. Quick Reference Commands

### Docker Commands
```bash
# Start all services
docker compose up -d

# Stop all services
docker compose down

# View logs
docker compose logs -f

# Rebuild specific service
docker compose up -d --build backend

# Restart service
docker compose restart backend

# Execute command in container
docker exec -it coffee_backend sh
```

### Backend Commands
```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Run tests
dotnet test
```

### Frontend Commands
```bash
# Install dependencies
npm install

# Run dev server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

### Database Commands
```bash
# Connect to PostgreSQL
docker exec -it coffee_postgres psql -U coffee_user -d coffee_analytics

# Connect to Redis
docker exec -it coffee_redis redis-cli -a RedisPassword123!
```

---

## 17. Support and Resources

### Documentation
- .NET Documentation: https://learn.microsoft.com/en-us/dotnet/
- React Documentation: https://react.dev/
- Docker Documentation: https://docs.docker.com/
- PostgreSQL Documentation: https://www.postgresql.org/docs/

### Useful Tools
- Postman: API testing
- DBeaver: Database management
- VS Code: Code editor
- GitKraken: Git GUI

### Getting Help
- Check logs: `docker compose logs -f`
- Check documentation in code comments
- Ask team members
- Create issue in project repository

---

## 18. Summary

You've now learned how to:

✅ Set up development environment  
✅ Configure environment variables  
✅ Set up database and Redis  
✅ Run backend and frontend  
✅ Use Docker Compose for orchestration  
✅ Debug common issues  
✅ Deploy to production  

**Next Steps**:
1. Start the system: `docker compose up -d`
2. Access frontend: http://localhost
3. Explore the application
4. Read code to understand architecture
5. Start contributing!

**Good luck and happy coding! 🚀**
