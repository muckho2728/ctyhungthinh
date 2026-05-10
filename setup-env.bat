@echo off
REM ============================================================
REM Coffee Analytics Platform - Simple Environment Setup
REM ============================================================

echo.
echo ========================================
echo Environment Configuration Setup
echo ========================================
echo.

REM Check if .env exists
if not exist .env (
    echo Creating .env from .env.example...
    copy .env.example .env >nul
    echo .env file created
) else (
    echo .env file already exists, backing up...
    copy .env .env.backup >nul
)

echo.
echo Please manually edit .env file and update:
echo.
echo   TWELVEDATA_API_KEY=c5795c825e5447c8a05a7cfe6c5da761
echo   JWT_SECRET= [Generate with: openssl rand -base64 32 or use a random 32+ char string]
echo.
echo Opening .env file in notepad...
notepad .env

echo.
echo After editing, press any key to continue...
pause >nul

echo.
echo Configuration complete!
echo You can now start the system with: docker compose up -d
echo.
