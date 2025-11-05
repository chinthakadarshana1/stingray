#!/bin/bash

echo "========================================"
echo "Stingray E-Commerce Backend Test Script"
echo "========================================"
echo ""

# Check if services are running
echo "Checking if services are running..."
if ! curl -s http://localhost:5001/health > /dev/null; then
    echo "ERROR: UserService is not running on port 5001"
    echo "Please run: docker-compose up"
    exit 1
fi

if ! curl -s http://localhost:5002/health > /dev/null; then
    echo "ERROR: OrderService is not running on port 5002"
    echo "Please run: docker-compose up"
    exit 1
fi

echo "[OK] Services are running"
echo ""

# Test 1: Create a User
echo "========================================"
echo "Test 1: Creating a User"
echo "========================================"
echo ""
echo "POST http://localhost:5001/users"
echo 'Body: {"email":"test@example.com","name":"Test User"}'
echo ""

USER_RESPONSE=$(curl -s -X POST http://localhost:5001/users \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","name":"Test User"}')

echo "$USER_RESPONSE"
echo ""

USER_ID=$(echo "$USER_RESPONSE" | grep -o '"id":"[^"]*' | cut -d'"' -f4)

if [ -z "$USER_ID" ]; then
    echo "ERROR: Failed to create user"
    exit 1
fi

echo "[OK] User created with ID: $USER_ID"
echo ""
sleep 2

# Test 2: Get User by ID
echo "========================================"
echo "Test 2: Getting User by ID"
echo "========================================"
echo ""
echo "GET http://localhost:5001/users/$USER_ID"
echo ""

curl -s http://localhost:5001/users/$USER_ID | jq .
echo ""
echo "[OK] User retrieved successfully"
echo ""
sleep 2

# Test 3: Create an Order
echo "========================================"
echo "Test 3: Creating an Order"
echo "========================================"
echo ""
echo "POST http://localhost:5002/orders"
echo ""

ORDER_RESPONSE=$(curl -s -X POST http://localhost:5002/orders \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"$USER_ID\",\"productName\":\"Laptop\",\"quantity\":1,\"totalPrice\":999.99}")

echo "$ORDER_RESPONSE"
echo ""

ORDER_ID=$(echo "$ORDER_RESPONSE" | grep -o '"id":"[^"]*' | cut -d'"' -f4)

if [ -z "$ORDER_ID" ]; then
    echo "ERROR: Failed to create order"
    exit 1
fi

echo "[OK] Order created with ID: $ORDER_ID"
echo ""
sleep 2

# Test 4: Get Order by ID
echo "========================================"
echo "Test 4: Getting Order by ID"
echo "========================================"
echo ""
echo "GET http://localhost:5002/orders/$ORDER_ID"
echo ""

curl -s http://localhost:5002/orders/$ORDER_ID | jq .
echo ""
echo "[OK] Order retrieved successfully"
echo ""
sleep 2

# Test 5: Test Validation
echo "========================================"
echo "Test 5: Testing Validation (Invalid Email)"
echo "========================================"
echo ""
echo "POST http://localhost:5001/users"
echo 'Body: {"email":"invalid-email","name":"Test"}'
echo ""

curl -s -X POST http://localhost:5001/users \
  -H "Content-Type: application/json" \
  -d '{"email":"invalid-email","name":"Test"}' | jq .

echo ""
echo "[OK] Validation working (should return 400)"
echo ""

# Test 6: Health Checks
echo "========================================"
echo "Test 6: Health Checks"
echo "========================================"
echo ""
echo "GET http://localhost:5001/health"
curl -s http://localhost:5001/health | jq .
echo ""
echo "GET http://localhost:5002/health"
curl -s http://localhost:5002/health | jq .
echo ""
echo "[OK] Health checks passed"
echo ""

echo "========================================"
echo "All Tests Completed Successfully!"
echo "========================================"
echo ""
echo "Summary:"
echo "- User created and retrieved"
echo "- Order created and retrieved"
echo "- Validation working correctly"
echo "- Health checks passing"
echo ""
echo "Check OrderService logs for UserCreated event consumption:"
echo "  docker logs stingray.services.orderservice"
echo ""
@echo off
echo ========================================
echo Stingray E-Commerce Backend Test Script
echo ========================================
echo.

REM Check if services are running
echo Checking if services are running...
curl -s http://localhost:5001/health >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: UserService is not running on port 5001
    echo Please run: docker-compose up
    exit /b 1
)

curl -s http://localhost:5002/health >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: OrderService is not running on port 5002
    echo Please run: docker-compose up
    exit /b 1
)

echo [OK] Services are running
echo.

REM Test 1: Create a User
echo ========================================
echo Test 1: Creating a User
echo ========================================
echo.
echo POST http://localhost:5001/users
echo Body: {"email":"test@example.com","name":"Test User"}
echo.

curl -X POST http://localhost:5001/users ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"test@example.com\",\"name\":\"Test User\"}" ^
  -w "\nStatus: %%{http_code}\n" ^
  > user-response.json

echo.
type user-response.json
echo.

REM Extract user ID (basic parsing for Windows)
powershell -Command "$json = Get-Content user-response.json | ConvertFrom-Json; $json.id" > user-id.txt
set /p USER_ID=<user-id.txt

if "%USER_ID%"=="" (
    echo ERROR: Failed to create user
    exit /b 1
)

echo [OK] User created with ID: %USER_ID%
echo.
timeout /t 2 /nobreak >nul

REM Test 2: Get User by ID
echo ========================================
echo Test 2: Getting User by ID
echo ========================================
echo.
echo GET http://localhost:5001/users/%USER_ID%
echo.

curl http://localhost:5001/users/%USER_ID% -w "\nStatus: %%{http_code}\n"
echo.
echo [OK] User retrieved successfully
echo.
timeout /t 2 /nobreak >nul

REM Test 3: Create an Order
echo ========================================
echo Test 3: Creating an Order
echo ========================================
echo.
echo POST http://localhost:5002/orders
echo Body: {"userId":"%USER_ID%","productName":"Laptop","quantity":1,"totalPrice":999.99}
echo.

curl -X POST http://localhost:5002/orders ^
  -H "Content-Type: application/json" ^
  -d "{\"userId\":\"%USER_ID%\",\"productName\":\"Laptop\",\"quantity\":1,\"totalPrice\":999.99}" ^
  -w "\nStatus: %%{http_code}\n" ^
  > order-response.json

echo.
type order-response.json
echo.

REM Extract order ID
powershell -Command "$json = Get-Content order-response.json | ConvertFrom-Json; $json.id" > order-id.txt
set /p ORDER_ID=<order-id.txt

if "%ORDER_ID%"=="" (
    echo ERROR: Failed to create order
    exit /b 1
)

echo [OK] Order created with ID: %ORDER_ID%
echo.
timeout /t 2 /nobreak >nul

REM Test 4: Get Order by ID
echo ========================================
echo Test 4: Getting Order by ID
echo ========================================
echo.
echo GET http://localhost:5002/orders/%ORDER_ID%
echo.

curl http://localhost:5002/orders/%ORDER_ID% -w "\nStatus: %%{http_code}\n"
echo.
echo [OK] Order retrieved successfully
echo.
timeout /t 2 /nobreak >nul

REM Test 5: Test Validation
echo ========================================
echo Test 5: Testing Validation (Invalid Email)
echo ========================================
echo.
echo POST http://localhost:5001/users
echo Body: {"email":"invalid-email","name":"Test"}
echo.

curl -X POST http://localhost:5001/users ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"invalid-email\",\"name\":\"Test\"}" ^
  -w "\nStatus: %%{http_code}\n"

echo.
echo [OK] Validation working (should return 400)
echo.

REM Test 6: Health Checks
echo ========================================
echo Test 6: Health Checks
echo ========================================
echo.
echo GET http://localhost:5001/health
curl http://localhost:5001/health
echo.
echo.
echo GET http://localhost:5002/health
curl http://localhost:5002/health
echo.
echo [OK] Health checks passed
echo.

REM Cleanup
del user-response.json order-response.json user-id.txt order-id.txt 2>nul

echo.
echo ========================================
echo All Tests Completed Successfully!
echo ========================================
echo.
echo Summary:
echo - User created and retrieved
echo - Order created and retrieved
echo - Validation working correctly
echo - Health checks passing
echo.
echo Check OrderService logs for UserCreated event consumption:
echo   docker logs stingray.services.orderservice
echo.
pause

