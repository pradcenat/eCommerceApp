# eCommerce Microservices Application
## Clean Architecture | .NET 8 | Azure Service Bus | Redis | Polly | Ocelot

---

## Architecture Overview

```
Client Request
      ↓
API Gateway (Ocelot) — Port 5000
JWT Validation + Routing
      ↓
┌─────────────────────────────────────┐
│                                     │
UserService      ProductService    OrderService
Port 5001        Port 5002         Port 5003
SQL Server       SQL Server        SQL Server
                 Redis Cache       Service Bus
                                   Polly Circuit Breaker
```

---

## Services

| Service | Port | Database | Features |
|---|---|---|---|
| API Gateway | 5000 | None | Ocelot, JWT Validation |
| UserService | 5001 | eCommerce_UserDb | Register, Login, JWT |
| ProductService | 5002 | eCommerce_ProductDb | CRUD, Redis Cache |
| OrderService | 5003 | eCommerce_OrderDb | Orders, Service Bus, Polly |

---

## Prerequisites

Before running the application install and configure:

1. **.NET 8 SDK** — https://dotnet.microsoft.com/download
2. **SQL Server** — Local instance running
3. **Redis** — Install Redis locally
   - Windows: https://github.com/microsoftarchive/redis/releases
   - Or use Docker: `docker run -d -p 6379:6379 redis`
4. **Azure Service Bus** — Create namespace in Azure Portal
   - Create queue: `order-placed-queue`
   - Create queue: `order-status-queue`
   - Copy connection string

---

## Setup Instructions

### Step 1 — Update Connection Strings

Update SQL Server connection string in each service appsettings.json.

Replace `DESKTOP-5L3O3K9` with your SQL Server instance name:

**UserService/UserService.Api/appsettings.json:**
```json
"UserDb": "Server=YOUR_SERVER;Database=eCommerce_UserDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

**ProductService/ProductService.Api/appsettings.json:**
```json
"ProductDb": "Server=YOUR_SERVER;Database=eCommerce_ProductDb;Trusted_Connection=True;TrustServerCertificate=True;",
"Redis": "localhost:6379"
```

**OrderService/OrderService.Api/appsettings.json:**
```json
"OrderDb": "Server=YOUR_SERVER;Database=eCommerce_OrderDb;Trusted_Connection=True;TrustServerCertificate=True;",
"ServiceBus": "YOUR_SERVICE_BUS_CONNECTION_STRING"
```

### Step 2 — Run Database Migrations

Each service auto-migrates on startup in Development mode.
Or run manually for each service:

```bash
# UserService
cd UserService/UserService.Api
dotnet ef database update --project ../UserService.Infrastructure

# ProductService
cd ProductService/ProductService.Api
dotnet ef database update --project ../ProductService.Infrastructure

# OrderService
cd OrderService/OrderService.Api
dotnet ef database update --project ../OrderService.Infrastructure
```

### Step 3 — Run All Services

Open 4 terminal windows and run each service:

**Terminal 1 — API Gateway:**
```bash
cd ApiGateway
dotnet run
```

**Terminal 2 — UserService:**
```bash
cd UserService/UserService.Api
dotnet run
```

**Terminal 3 — ProductService:**
```bash
cd ProductService/ProductService.Api
dotnet run
```

**Terminal 4 — OrderService:**
```bash
cd OrderService/OrderService.Api
dotnet run
```

### Step 4 — Test the Application

**Swagger URLs:**
- API Gateway: https://localhost:5000
- UserService: https://localhost:5001
- ProductService: https://localhost:5002
- OrderService: https://localhost:5003

---

## API Flow — How To Test End To End

### 1. Register a User
```
POST https://localhost:5001/api/user/register
{
  "firstName": "Pradeep",
  "lastName": "Kumar",
  "email": "pradeep@email.com",
  "password": "Test@1234"
}
```

### 2. Login and Get JWT Token
```
POST https://localhost:5001/api/auth/login
{
  "email": "pradeep@email.com",
  "password": "Test@1234"
}
Response includes JWT token — copy this token
```

### 3. Use Token for All Other Requests
```
Header: Authorization: Bearer {your_token}
```

### 4. Get All Products (Redis Cached)
```
GET https://localhost:5002/api/product
First call — loads from SQL Server, stores in Redis
Second call — loads from Redis (much faster)
```

### 5. Create an Order
```
POST https://localhost:5003/api/order
Authorization: Bearer {token}
{
  "userId": "your-user-id",
  "userEmail": "pradeep@email.com",
  "shippingAddress": "123 Main Street, Bengaluru 560001",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "productName": "Blue Denim Jeans",
      "unitPrice": 59.99,
      "quantity": 2
    }
  ]
}
```

After creating order:
- Order saved in OrderService database
- OrderPlaced event published to Azure Service Bus
- ProductService subscribes and reduces stock automatically

### 6. Via API Gateway (routes all requests)
```
POST https://localhost:5000/api/auth/login       → routes to UserService
GET  https://localhost:5000/api/product           → routes to ProductService
POST https://localhost:5000/api/order             → routes to OrderService
```

---

## Key Features Explained

### Clean Architecture
Each service has 4 layers:
- **Domain** — Entities only, no dependencies
- **Application** — Business logic, interfaces, DTOs, validators
- **Infrastructure** — EF Core, Redis, Service Bus implementations
- **API** — Controllers, middleware, Program.cs

### JWT Authentication
- Generated in UserService on login
- Validated in each service independently
- Validated at API Gateway level for all protected routes
- Contains userId, email, role claims

### Redis Cache (ProductService)
- All products cached for 30 minutes
- Cache invalidated on create/update/delete
- Cache-aside pattern — check cache first, then DB
- Logs cache hit/miss for observability

### Azure Service Bus
- OrderService publishes OrderPlaced event on order creation
- Event contains order details and items
- ProductService would subscribe to reduce stock
- Decouples services — no direct HTTP calls for events

### Polly Circuit Breaker (OrderService)
- Wraps HTTP calls to ProductService
- Opens circuit after 50% failure rate
- Stays open for 30 seconds
- Half-open state tests recovery
- Prevents cascade failures

### FluentValidation
- Validates all requests before processing
- Returns structured error list on failure
- Applied in controllers before service calls

### Global Exception Middleware
- Catches all unhandled exceptions
- Maps exception types to HTTP status codes
- Returns consistent ApiResponse format
- Logs all exceptions with full details

---

## Project Structure

```
eCommerceApp/
├── eCommerceApp.sln
├── README.md
├── ApiGateway/
│   ├── ApiGateway.csproj
│   ├── Program.cs
│   ├── ocelot.json
│   └── appsettings.json
├── UserService/
│   ├── UserService.Domain/
│   ├── UserService.Application/
│   ├── UserService.Infrastructure/
│   └── UserService.Api/
├── ProductService/
│   ├── ProductService.Domain/
│   ├── ProductService.Application/
│   ├── ProductService.Infrastructure/  ← Redis here
│   └── ProductService.Api/
└── OrderService/
    ├── OrderService.Domain/
    ├── OrderService.Application/
    ├── OrderService.Infrastructure/    ← Service Bus + Polly here
    └── OrderService.Api/
```

---

## Technology Stack

| Technology | Purpose | Version |
|---|---|---|
| .NET | Framework | 8.0 |
| Entity Framework Core | ORM | 8.0.8 |
| SQL Server | Database | Latest |
| Redis | Caching | StackExchange.Redis 2.8.0 |
| Azure Service Bus | Messaging | 7.18.1 |
| Polly | Resilience | 8.4.1 |
| Ocelot | API Gateway | 23.3.3 |
| AutoMapper | Object Mapping | 12.0.1 |
| FluentValidation | Validation | 11.9.2 |
| BCrypt | Password Hashing | 4.0.3 |
| Swagger | API Documentation | 6.7.3 |
| JWT Bearer | Authentication | 8.0.8 |

---

## What To Build Next

- [ ] Docker and Docker Compose
- [ ] BasketService with Redis
- [ ] PaymentService
- [ ] NotificationService
- [ ] Outbox Pattern for reliable event publishing
- [ ] Health checks endpoint
- [ ] Rate limiting in API Gateway
- [ ] Distributed tracing with Application Insights
- [ ] Unit tests with xUnit and Moq

---

Built with Clean Architecture principles.
Every layer depends only inward.
Application never references Infrastructure directly.
```
