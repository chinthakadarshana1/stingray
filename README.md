# Stingray - Event-Driven E-Commerce Backend

A microservices-based e-commerce backend system built with .NET 8, demonstrating Clean Architecture, CQRS pattern, Event Sourcing with
Kafka, and the Outbox Pattern.

## Architecture Overview

This project follows Clean Architecture principles with clear separation of concerns:

> 📖 **For detailed architecture diagrams and in-depth explanation, see [ARCHITECTURE.md](doc/ARCHITECTURE.md)**

### Layers

1. **Domain Layer** (`Stingray.Domain`)
    - Core business entities (User, Order)
    - Domain events (UserCreatedEvent, OrderCreatedEvent)
    - Repository interfaces
    - Outbox pattern entities

2. **Application Layer** (`Stingray.Application`)
    - DTOs for API requests/responses
    - CQRS Commands and Queries
    - MediatR Command/Query Handlers
    - FluentValidation validators
    - Event publisher/consumer interfaces

3. **Infrastructure Layer**
    - `Stingray.Storage.InMemory` - Data persistence
        - EF Core DbContext
        - Repository implementations
        - In-memory database for development
    - `Stingray.Infrastructure.KafkaMessaging` ⭐ **NEW** - Kafka messaging infrastructure
        - Kafka-specific event publisher implementation
        - Message broker abstractions (IMessagePublisher)
        - Easily swappable for RabbitMQ, Azure Service Bus, etc.
        - Extension methods for DI registration

4. **Services Layer**
    - `Stingray.Services.UserService` - User management microservice (REST API)
    - `Stingray.Services.OrderService` - Order management microservice (REST API)
        - Polly-based resilience policies ⭐
        - MediatR notification handlers for consumed events
        - Automatic retry with exponential backoff
    - `Stingray.Services.OutboxProcessor` - **Standalone background worker** for reliable event publishing ⭐
        - Separately deployable Worker Service
        - Processes outbox messages from databases
        - Publishes events to Kafka with retry logic
        - Can be scaled independently

## Key Features

### 1. Clean Architecture

- Clear separation between domain logic, application logic, and infrastructure
- Domain entities are independent of external concerns
- Dependency inversion principle applied throughout

### 2. CQRS Pattern

- Commands for write operations (CreateUserCommand, CreateOrderCommand)
- Queries for read operations (GetUserByIdQuery, GetOrderByIdQuery)
- MediatR for handling commands and queries

### 3. Event-Driven Architecture

- Apache Kafka for event streaming
- Asynchronous communication between microservices
- Event publishing on entity creation

### 4. Outbox Pattern

- Ensures reliable event publishing
- Events saved to database before publishing to Kafka
- Background processor retries failed events
- Guarantees at-least-once delivery

### 5. Minimal APIs (.NET 8)

- Lightweight HTTP API endpoints
- OpenAPI/Swagger documentation
- Fluent validation integration

### 6. Resilience with Polly ⭐ **NEW**

- Industry-standard resilience library
- Exponential backoff retry strategy
- Handles transient Kafka connection failures
- Configurable via Dependency Injection
- Automatic retry for broker down scenarios

### 7. Message Broker Abstraction ⭐ **NEW**

- Clean abstraction over message brokers (IMessagePublisher)
- Kafka implementation in dedicated infrastructure project
- Easy to swap Kafka for RabbitMQ, Azure Service Bus, AWS SQS
- Single line configuration change to switch brokers

### 8. Containerization

- Docker support for all services
- Docker Compose orchestration with health checks
- Kafka and Zookeeper containers
- Services wait for dependencies to be healthy

## Project Structure

```
Stingray/
├── Stingray.Domain/
│   ├── User.cs
│   ├── Order.cs
│   ├── Events/
│   │   ├── UserCreatedEvent.cs
│   │   └── OrderCreatedEvent.cs
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── IOrderRepository.cs
│   │   └── IOutboxRepository.cs
│   └── Outbox/
│       └── OutboxMessage.cs
│
├── Stingray.Application/
│   ├── DTOs/
│   │   ├── CreateUserRequest.cs
│   │   ├── UserResponse.cs
│   │   ├── CreateOrderRequest.cs
│   │   └── OrderResponse.cs
│   ├── Commands/
│   │   ├── CreateUserCommand.cs
│   │   └── CreateOrderCommand.cs
│   ├── Queries/
│   │   ├── GetUserByIdQuery.cs
│   │   └── GetOrderByIdQuery.cs
│   ├── Handlers/
│   │   ├── CreateUserCommandHandler.cs
│   │   ├── GetUserByIdQueryHandler.cs
│   │   ├── CreateOrderCommandHandler.cs
│   │   └── GetOrderByIdQueryHandler.cs
│   ├── Validators/
│   │   ├── CreateUserCommandValidator.cs
│   │   └── CreateOrderCommandValidator.cs
│   └── Interfaces/
│       ├── IEventPublisher.cs
│       └── IEventConsumer.cs
│
├── Stingray.Storage.InMemory/
│   ├── StingrayDbContext.cs
│   └── Repositories/
│       ├── UserRepository.cs
│       ├── OrderRepository.cs
│       └── OutboxRepository.cs
│
├── Stingray.Infrastructure.KafkaMessaging/ ⭐ NEW
│   ├── KafkaEventPublisher.cs (with Outbox pattern)
│   ├── KafkaMessagePublisher.cs (direct publishing)
│   ├── ServiceCollectionExtensions.cs
│   └── README.md
│
├── Stingray.Services.UserService/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
├── Stingray.Services.OrderService/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   └── Infrastructure/
│       └── UserCreatedEventConsumer.cs (with Polly resilience) ⭐
│
└── Stingray.Services.OutboxProcessor/ ⭐ STANDALONE
    ├── Program.cs
    ├── OutboxProcessor.cs
    ├── appsettings.json
    ├── Dockerfile
    └── README.md
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker Desktop
- Docker Compose

### Running the Application

1. **Clone the repository**
   ```bash
   cd C:\cHiNProjects\temp\stingray\src
   ```

2. **Build and run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

   This will start:
    - Zookeeper (port 2181)
    - Kafka (ports 9092, 29092) - with health checks ⭐
    - UserService (port 5001) - waits for Kafka to be healthy
    - OrderService (port 5002) - waits for Kafka to be healthy
    - OutboxProcessor (background worker) - waits for Kafka to be healthy

3. **Access the APIs**
    - UserService Swagger: http://localhost:5001/swagger
    - OrderService Swagger: http://localhost:5002/swagger

### API Endpoints

#### UserService (http://localhost:5001)

**Create User**

```http
POST /users
Content-Type: application/json

{
  "email": "user@example.com",
  "name": "John Doe"
}
```

**Get User by ID**

```http
GET /users/{id}
```

**Health Check**

```http
GET /health
```

#### OrderService (http://localhost:5002)

**Create Order**

```http
POST /orders
Content-Type: application/json

{
  "userId": "guid-here",
  "productName": "Product Name",
  "quantity": 2,
  "totalPrice": 99.99
}
```

**Get Order by ID**

```http
GET /orders/{id}
```

**Health Check**

```http
GET /health
```

## Event Flow

1. **User Creation Flow**
    - Client sends POST request to `/users`
    - UserService validates and creates user
    - UserCreatedEvent is saved to outbox
    - Event is published to Kafka topic "UserCreated"
    - OrderService consumes the event and logs it

2. **Order Creation Flow**
    - Client sends POST request to `/orders`
    - OrderService validates and creates order
    - OrderCreatedEvent is saved to outbox
    - Event is published to Kafka topic "OrderCreated"

3. **Outbox Pattern Flow**
    - Events are first saved to OutboxMessages table
    - Immediate publish attempt is made
    - If publish fails, OutboxProcessor retries every 5 seconds
    - Processed messages are marked with ProcessedAt timestamp

## Technical Highlights

### Validation

- FluentValidation for input validation
- Automatic validation on API endpoints
- Detailed validation error responses

### Database

- EF Core with In-Memory provider
- Separate database per microservice
- DbContext with proper entity configuration

### Event Publishing

- Transactional outbox pattern
- Background service for retry logic
- JSON serialization for event payloads

### Kafka Integration

- Confluent.Kafka client library
- Auto-topic creation enabled
- Consumer groups for scalability
- Health checks for broker availability ⭐
- Services wait for Kafka to be ready before starting ⭐

### Resilience & Fault Tolerance ⭐ **NEW**

- **Polly** for resilience policies
- Exponential backoff retry strategy
- Transient fault handling
- Circuit breaker ready (can be added)
- Timeout policies (can be added)

### Message Broker Abstraction ⭐ **NEW**

- `IMessagePublisher` interface for broker-agnostic code
- `Stingray.Infrastructure.KafkaMessaging` - Kafka implementation
- Easy to swap: Kafka → RabbitMQ → Azure Service Bus
- Extension methods: `AddKafkaMessaging()`

### MediatR Integration ⭐ **NEW**

- Event handlers as MediatR notification handlers
- Clean separation: Infrastructure consumes, Application processes
- No Kafka dependencies in Application layer
- Scoped service resolution in background services

### Observability

- Structured logging with ILogger
- Health check endpoints
- Swagger/OpenAPI documentation
- Retry attempt logging with Polly

## Configuration

### Kafka Configuration

Services use `appsettings.json` for configuration:

```json
{
  "Kafka": {
    "BootstrapServers": "kafka:9092"
  }
}
```

Override via environment variables in docker-compose.yml:

```yaml
environment:
  - Kafka__BootstrapServers=kafka:9092
```

### Resilience Configuration ⭐ **NEW**

Polly resilience policies are configured in `Program.cs`:

```csharp
builder.Services.AddSingleton<ResiliencePipeline>(sp =>
{
    return new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = int.MaxValue,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            MaxDelay = TimeSpan.FromSeconds(30)
        })
        .Build();
});
```

**Features:**
- Automatic retry on transient failures (broker down, topic not available)
- Exponential backoff: 2s, 4s, 8s, 16s, 30s (max)
- Infinite retries with structured logging
- Handles: UnknownTopicOrPart, Local_AllBrokersDown, BrokerNotAvailable

## Testing the System

### Test User Creation and Event Flow

1. **Create a User**
   ```bash
   curl -X POST http://localhost:5001/users \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","name":"Test User"}'
   ```

2. **Verify User Created**
   ```bash
   curl http://localhost:5001/users/{user-id}
   ```

3. **Check OrderService Logs**
   ```bash
   docker logs kafka
   docker logs stingray.services.orderservice
   ```
   You should see the UserCreated event being consumed.

4. **Create an Order**
   ```bash
   curl -X POST http://localhost:5002/orders \
     -H "Content-Type: application/json" \
     -d '{
       "userId": "{user-id}",
       "productName": "Sample Product",
       "quantity": 2,
       "totalPrice": 29.99
     }'
   ```

### Verify Outbox Pattern

1. Stop Kafka temporarily to simulate failure
2. Create a user - it will save to outbox but fail to publish
3. Restart Kafka
4. OutboxProcessor will automatically retry and publish the event

## Design Patterns Used

1. **Clean Architecture** - Separation of concerns across layers
2. **CQRS** - Separate read and write models
3. **Mediator Pattern** - Via MediatR library
4. **Repository Pattern** - Data access abstraction
5. **Outbox Pattern** - Reliable event publishing
6. **Event Sourcing** - Domain events for state changes
7. **Dependency Injection** - Built-in .NET DI container

## Future Enhancements

- [ ] Add authentication and authorization (JWT)
- [ ] Implement distributed tracing (OpenTelemetry)
- [ ] Add unit and integration tests
- [ ] Implement API Gateway (YARP or Ocelot)
- [ ] Add saga pattern for distributed transactions
- [ ] Implement CQRS with separate read/write databases
- [ ] Add monitoring with Prometheus and Grafana
- [ ] Implement rate limiting and circuit breakers
- [ ] Add message deduplication
- [ ] Implement event versioning

## Architecture

For comprehensive architecture documentation, diagrams, and deployment guides, see:

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Complete system architecture with visual diagrams
  - Container architecture
  - Data flow diagrams
  - Deployment options
  - Scaling strategies
  - Component responsibilities

- **[OUTBOX_STANDALONE_GUIDE.md](OUTBOX_STANDALONE_GUIDE.md)** - OutboxProcessor deployment guide
  - Standalone deployment options
  - Configuration examples
  - Monitoring and troubleshooting
  - Kubernetes manifests

- **[STANDALONE_COMPLETE.md](STANDALONE_COMPLETE.md)** - OutboxProcessor implementation summary
  - What changed from embedded to standalone
  - Benefits and advantages
  - Quick reference commands

- **[ARCHITECTURE_FINAL.md](ARCHITECTURE_FINAL.md)** - Detailed architecture with all components
  - System overview diagrams
  - Technology stack breakdown
  - Network communication
  - Scaling strategies

### Refactoring & Implementation Guides ⭐ **NEW**

- **[REFACTORING_MEDIATR_PATTERN.md](REFACTORING_MEDIATR_PATTERN.md)** - Event consumer refactoring with MediatR
  - Moving business logic to Application layer
  - MediatR notification pattern
  - Separation of infrastructure and business logic

- **[REFACTORING_KAFKA_INFRASTRUCTURE.md](REFACTORING_KAFKA_INFRASTRUCTURE.md)** - Kafka infrastructure extraction
  - Creating dedicated KafkaMessaging project
  - Message broker abstraction
  - How to swap Kafka for RabbitMQ

- **[REFACTORING_POLLY_INTEGRATION.md](REFACTORING_POLLY_INTEGRATION.md)** - Polly resilience integration
  - Replacing custom retry logic with Polly
  - Exponential backoff configuration
  - Handling transient failures

- **[REFACTORING_POLLY_DI.md](REFACTORING_POLLY_DI.md)** - Moving Polly to Dependency Injection
  - Centralized resilience configuration
  - Testability improvements
  - Reusable resilience pipelines

## Technologies Used

- **.NET 8** - Latest LTS version
- **ASP.NET Core Minimal APIs** - Lightweight HTTP APIs
- **Entity Framework Core 8** - ORM
- **MediatR** - Mediator pattern implementation
- **FluentValidation** - Input validation
- **Polly** ⭐ - Resilience and transient-fault-handling
- **Confluent.Kafka** - Kafka client
- **Apache Kafka** - Event streaming platform
- **Docker & Docker Compose** - Containerization with health checks
- **Swagger/OpenAPI** - API documentation

## License

This project is a demonstration of clean architecture and event-driven patterns.

## Author

Built following best practices for microservices architecture and event-driven systems.



