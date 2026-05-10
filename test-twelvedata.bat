@echo off
REM ============================================================
REM TwelveData API Integration Test
REM Tests the TwelveData API with provided key
REM ============================================================

echo.
echo ========================================
echo TwelveData API Integration Test
echo ========================================
echo.

set API_KEY=c5795c825e5447c8a05a7cfe6c5da761
set BASE_URL=https://api.twelvedata.com

echo API Key: %API_KEY%
echo Base URL: %BASE_URL%
echo.

REM Test 1: Real-time Price
echo [Test 1] Testing real-time price endpoint (AAPL)...
curl -s "%BASE_URL%/price?symbol=AAPL&apikey=%API_KEY%"
echo.
echo.

REM Test 2: Time Series
echo [Test 2] Testing time series endpoint (AAPL)...
curl -s "%BASE_URL%/time_series?symbol=AAPL&interval=1day&outputsize=5&apikey=%API_KEY%"
echo.
echo.

REM Test 3: Quote
echo [Test 3] Testing quote endpoint (AAPL)...
curl -s "%BASE_URL%/quote?symbol=AAPL&apikey=%API_KEY%"
echo.
echo.

REM Test 4: RSI Indicator
echo [Test 4] Testing RSI indicator endpoint (AAPL)...
curl -s "%BASE_URL%/rsi?symbol=AAPL&interval=1day&time_period=14&apikey=%API_KEY%"
echo.
echo.

REM Test 5: SMA Indicator
echo [Test 5] Testing SMA indicator endpoint (AAPL)...
curl -s "%BASE_URL%/sma?symbol=AAPL&interval=1day&time_period=20&apikey=%API_KEY%"
echo.
echo.

echo ========================================
echo Test complete!
echo ========================================
echo.
echo If you see JSON responses above, the API is working correctly.
echo If you see error messages, check:
echo   - API key is valid
echo   - Internet connection is active
echo   - TwelveData service is operational
echo.
pause
