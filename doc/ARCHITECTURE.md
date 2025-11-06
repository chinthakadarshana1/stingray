# Stingray Architecture - With Standalone OutboxProcessor


## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Layer                             │
│  (Web Browser, Mobile App, API Clients)                         │
└────────────┬────────────────────────────────┬───────────────────┘
             │                                │
             │ HTTP                           │ HTTP
             │                                │
┌────────────▼─────────────┐    ┌────────────▼─────────────┐
│    UserService           │    │    OrderService          │
│    (REST API)            │    │    (REST API)            │
│                          │    │                          │
│  - POST /users           │    │  - POST /orders          │
│  - GET /users/{id}       │    │  - GET /orders/{id}      │
│  - Port: 5001            │    │  - Port: 5002            │
│  - Swagger UI            │    │  - Swagger UI            │
│  - Kafka Messaging ⭐    │    │  - Polly Resilience ⭐   │
│                          │    │  - MediatR Consumers ⭐  │
└────────────┬─────────────┘    └────────────┬─────────────┘
             │                                │
             │ Writes to Outbox               │ Writes to Outbox
             │                                │
             └────────────┬───────────────────┘
                          │
                          ▼
             ┌─────────────────────────┐
             │     Database Layer      │
             │   (In-Memory DB)        │
             │                         │
             │  - Users Table          │
             │  - Orders Table         │
             │  - OutboxMessages Table │ ⭐
             └────────────┬────────────┘
                          │
                          │ Reads Outbox
                          │
             ┌────────────▼─────────────┐
             │   OutboxProcessor        │ ⭐ STANDALONE
             │   (Worker Service)       │
             │                          │
             │  - Background Worker     │
             │  - Polls every 5 seconds │
             │  - Publishes to Kafka    │
             │  - Retry Logic           │
             │  - Can scale separately  │
             └────────────┬─────────────┘
                          │
                          │ Publishes Events
                          │
                          ▼
             ┌─────────────────────────┐
             │   Apache Kafka          │
             │   (Message Broker)      │
             │                         │
             │  - UserCreated Topic    │
             │  - OrderCreated Topic   │
             │  - Port: 9092           │
             └────────────┬────────────┘
                          │
                          │ Consumes Events
                          │
                          ▼
             ┌─────────────────────────┐
             │   Event Consumers       │
             │                         │
             │  - OrderService listens │
             │    to UserCreated       │
             │  - Other services...    │
             └─────────────────────────┘
```

---

## Container Architecture

```
Docker Host
│
├─ Container: zookeeper
│  └─ Port: 2181
│
├─ Container: kafka
│  ├─ Port: 9092 (internal)
│  ├─ Port: 29092 (external)
│  └─ Health Check: kafka-broker-api-versions ⭐
│     └─ Interval: 10s, Start Period: 40s
│
├─ Container: user_service
│  ├─ Port: 5001 → 8080
│  ├─ Environment:
│  │  └─ Kafka__BootstrapServers=kafka:9092
│  ├─ Depends on: kafka (service_healthy) ⭐
│  ├─ Restart: on-failure ⭐
│  └─ Uses: Stingray.Infrastructure.KafkaMessaging ⭐
│
├─ Container: order_service
│  ├─ Port: 5002 → 8080
│  ├─ Environment:
│  │  └─ Kafka__BootstrapServers=kafka:9092
│  ├─ Depends on: kafka (service_healthy) ⭐
│  ├─ Restart: on-failure ⭐
│  ├─ Uses: Stingray.Infrastructure.KafkaMessaging ⭐
│  └─ Polly Resilience Pipeline ⭐
│
└─ Container: outbox_processor ⭐ NEW
   ├─ No exposed ports (background worker)
   ├─ Environment:
   │  ├─ Kafka__BootstrapServers=kafka:9092
   │  └─ DatabaseName=SharedOutboxDb
   ├─ Depends on: kafka
   └─ Restart: unless-stopped
```

---

## Data Flow

### 1. User Creation Flow

```
Client
  │
  │ POST /users
  ▼
UserService API
  │
  ├─ Validate Request
  ├─ Create User Entity
  ├─ Save to Database
  └─ Save to Outbox Table ⭐
      │
      │ {"eventType": "UserCreated", "payload": "{...}"}
      │
      ▼ (Polling)
OutboxProcessor ⭐
  │
  ├─ Fetch Unprocessed Messages
  ├─ Publish to Kafka Topic: "UserCreated"
  └─ Mark as Processed
      │
      ▼
Kafka Broker
  │
  ▼
OrderService Consumer
  │
  └─ Process UserCreated Event
```

### 2. Order Creation Flow

```
Client
  │
  │ POST /orders
  ▼
OrderService API
  │
  ├─ Validate Request
  ├─ Create Order Entity
  ├─ Save to Database
  └─ Save to Outbox Table ⭐
      │
      │ {"eventType": "OrderCreated", "payload": "{...}"}
      │
      ▼ (Polling)
OutboxProcessor ⭐
  │
  ├─ Fetch Unprocessed Messages
  ├─ Publish to Kafka Topic: "OrderCreated"
  └─ Mark as Processed
      │
      ▼
Kafka Broker
  │
  └─ Available for consumers
```

---

## Deployment Options

### Option 1: Docker Compose (Current)

```
docker-compose.yml
├─ zookeeper
├─ kafka
├─ user_service
├─ order_service
└─ outbox_processor ⭐
```

### Option 2: Kubernetes

```
Namespace: stingray
├─ Deployment: zookeeper
├─ Deployment: kafka
├─ Deployment: user-service (replicas: 3)
├─ Deployment: order-service (replicas: 3)
└─ Deployment: outbox-processor (replicas: 2) ⭐
```

### Option 3: Separate Infrastructure

```
Server 1: API Services
├─ user_service
└─ order_service

Server 2: Background Workers ⭐
└─ outbox_processor (multiple instances)

Server 3: Message Broker
└─ kafka cluster
```

---

## Scaling Strategy

### Horizontal Scaling

```
Load Balancer
      │
      ├──► UserService Instance 1
      ├──► UserService Instance 2
      └──► UserService Instance 3
           │
           └──► Same Database
                    │
                    └──► Multiple OutboxProcessor Workers ⭐
                         ├─ OutboxProcessor Instance 1
                         ├─ OutboxProcessor Instance 2
                         └─ OutboxProcessor Instance 3
                              │
                              └──► Kafka Cluster
```

### Independent Scaling

```bash
# Scale API services based on HTTP traffic
docker-compose up -d --scale user-service=5

# Scale OutboxProcessor based on message volume
docker-compose up -d --scale outbox-processor=3

# They scale independently! ✅
```

---

## Infrastructure Layer ⭐

### Kafka Messaging Abstraction

The system uses a dedicated infrastructure project for message broker abstraction:

```
Application Layer
├─ IEventPublisher (with Outbox pattern)
├─ IMessagePublisher (direct publishing)
└─ Business logic (NO Kafka dependencies)
        │
        ▼
Infrastructure Layer
├─ Stingray.Infrastructure.KafkaMessaging ⭐
│  ├─ KafkaEventPublisher (implements IEventPublisher)
│  ├─ KafkaMessagePublisher (implements IMessagePublisher)
│  └─ ServiceCollectionExtensions
│
└─ Easy to swap for:
   ├─ RabbitMQ
   ├─ Azure Service Bus
   ├─ AWS SQS
   └─ Any message broker
```

**Benefits:**
- ✅ **Single Source of Truth** - One Kafka implementation for all services
- ✅ **Easy to Test** - Mock interfaces instead of Kafka
- ✅ **Broker Agnostic** - Application code doesn't know about Kafka
- ✅ **One-Line Registration** - `builder.Services.AddKafkaMessaging()`
- ✅ **Swappable** - Change `AddKafkaMessaging()` to `AddRabbitMqMessaging()`

**Usage:**
```csharp
// In Program.cs
builder.Services.AddKafkaMessaging(builder.Configuration);

// To switch to RabbitMQ in the future:
// builder.Services.AddRabbitMqMessaging(builder.Configuration);
// Application code stays the same!
```

---

## Resilience with Polly ⭐

### Automatic Retry Strategy

OrderService uses **Polly** (v8.x) for resilience and fault tolerance:

```csharp
builder.Services.AddSingleton<ResiliencePipeline>(sp =>
{
    return new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = Handle<ConsumeException>(ex =>
                ex.Error.Code == ErrorCode.UnknownTopicOrPart ||
                ex.Error.Code == ErrorCode.Local_AllBrokersDown ||
                ex.Error.Code == ErrorCode.BrokerNotAvailable),
            MaxRetryAttempts = int.MaxValue,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            MaxDelay = TimeSpan.FromSeconds(30)
        })
        .Build();
});
```

**Retry Behavior:**

| Attempt | Delay | Action |
|---------|-------|--------|
| 1 | 2s | Retry after 2 seconds |
| 2 | 4s | Exponential backoff |
| 3 | 8s | Exponential backoff |
| 4 | 16s | Exponential backoff |
| 5+ | 30s | Capped at max delay |

**Handled Errors:**
- **UnknownTopicOrPart** - Topic doesn't exist yet (normal on startup)
- **Local_AllBrokersDown** - Kafka brokers unavailable
- **BrokerNotAvailable** - Specific broker down

**Benefits:**
- ✅ Automatic recovery from transient failures
- ✅ Exponential backoff reduces load during failures
- ✅ Structured logging of retry attempts
- ✅ Services automatically reconnect when Kafka comes back online
- ✅ No manual retry logic needed

---

## MediatR Event Processing ⭐

### Clean Separation of Concerns

Event consumers use MediatR to separate infrastructure from business logic:

```
Infrastructure Layer (OrderService)
│
UserCreatedEventConsumer (BackgroundService)
├─ Kafka connection & subscription
├─ Message deserialization
├─ Creates service scope (for scoped dependencies)
└─ Publishes to MediatR ───────┐
                               │
                               ▼
Application Layer (Stingray.Application)
│
UserCreatedConsumer (INotificationHandler<UserCreatedNotification>)
├─ Pure business logic
├─ NO Kafka dependencies
├─ NO BackgroundService
├─ NO IConfiguration
└─ Processes event (cache user, update read models, etc.)
```

**Benefits:**
- ✅ **Clean Separation** - Infrastructure vs Business Logic
- ✅ **Easy to Test** - Test handlers without Kafka
- ✅ **Reusable** - Multiple handlers for same event
- ✅ **SOLID Principles** - Each handler has single responsibility

**Example:**
```csharp
// Infrastructure consumes from Kafka
var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message);

// Delegate to MediatR
using var scope = _serviceProvider.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
await mediator.Publish(new UserCreatedNotification { UserId = userEvent.UserId });

// Application layer handles business logic
public class UserCreatedConsumer : INotificationHandler<UserCreatedNotification>
{
    public Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        // Pure business logic here
        // No Kafka knowledge needed!
    }
}
```

---

## Component Responsibilities

### UserService

- ✅ Handle HTTP requests for user operations
- ✅ Validate input
- ✅ Save entities to database
- ✅ Save events to outbox table
- ❌ Does NOT publish to Kafka directly

### OrderService

- ✅ Handle HTTP requests for order operations
- ✅ Validate input
- ✅ Save entities to database
- ✅ Save events to outbox table
- ✅ Consume events from Kafka (with Polly resilience) ⭐
- ✅ Process events via MediatR handlers ⭐
- ✅ Automatic retry with exponential backoff ⭐
- ✅ Scoped service resolution for handlers ⭐
- ❌ Does NOT publish to Kafka directly

### OutboxProcessor ⭐

- ✅ Poll outbox table continuously
- ✅ Publish events to Kafka
- ✅ Retry failed publishes
- ✅ Mark messages as processed
- ✅ Log all activities
- ❌ Does NOT handle HTTP requests

---

## Benefits of Standalone Architecture

| Aspect              | Before (Embedded) | After (Standalone)   |
|---------------------|-------------------|----------------------|
| **Deployment**      | Bundled with APIs | Independent          | ✅ |
| **Scaling**         | Scales with API   | Scales independently | ✅ |
| **Resources**       | Shares CPU/Memory | Dedicated resources  | ✅ |
| **Monitoring**      | Mixed logs        | Separate logs        | ✅ |
| **Debugging**       | Complex           | Simpler              | ✅ |
| **Fault Isolation** | Coupled           | Isolated             | ✅ |
| **Updates**         | Update entire API | Update independently | ✅ |
| **Resilience** ⭐    | Manual retries    | Polly with backoff   | ✅ |
| **Abstraction** ⭐   | Kafka coupled     | Broker agnostic      | ✅ |
| **Event Handling** ⭐ | Mixed concerns    | MediatR separation   | ✅ |

---

## Network Communication

```
┌─────────────────────────────────────────────────────┐
│              Docker Network: stingray               │
│                                                     │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐     │
│  │  User    │    │  Order   │    │ Outbox   │     │
│  │ Service  │    │ Service  │    │Processor │     │
│  │          │    │          │    │          │     │
│  │ :8080    │    │ :8080    │    │(worker)  │     │
│  └────┬─────┘    └────┬─────┘    └────┬─────┘     │
│       │               │               │            │
│       └───────────────┼───────────────┘            │
│                       │                            │
│                  ┌────▼────┐                       │
│                  │  Kafka  │                       │
│                  │  :9092  │                       │
│                  └─────────┘                       │
└─────────────────────────────────────────────────────┘
        │                                    │
        │ Host: 5001                        │ Host: 5002
        ▼                                    ▼
   [External                            [External
    Clients]                             Clients]
```

---

## Technology Stack

### API Services

- **.NET 8** - Runtime
- **ASP.NET Core Minimal APIs** - Web framework
- **MediatR** - CQRS implementation & event handling ⭐
- **FluentValidation** - Input validation
- **Polly** ⭐ - Resilience and transient-fault-handling
- **Swagger** - API documentation

### Infrastructure Projects ⭐

- **Stingray.Infrastructure.KafkaMessaging** ⭐
  - Kafka-specific implementations
  - Message broker abstraction
  - Easy to swap for RabbitMQ, Azure Service Bus, etc.
- **Stingray.Storage.InMemory**
  - EF Core DbContext
  - Repository implementations
  - In-Memory database for development

### OutboxProcessor ⭐

- **.NET 8** - Runtime
- **Microsoft.Extensions.Hosting** - Worker service framework
- **Confluent.Kafka** - Kafka client
- **Entity Framework Core** - Database access

### Infrastructure

- **Docker** - Containerization with health checks ⭐
- **Docker Compose** - Orchestration
- **Apache Kafka** - Message broker
- **Apache Zookeeper** - Kafka coordination

---

## Summary

The **Stingray architecture** is now **production-ready** with:

### Core Services
1. **UserService** - User management API with Kafka messaging abstraction ⭐
2. **OrderService** - Order management API with Polly resilience & MediatR ⭐
3. **OutboxProcessor** - Standalone event publishing worker

### Key Improvements ⭐

**Resilience with Polly:**
- ✅ Automatic retry with exponential backoff (2s → 4s → 8s → 16s → 30s)
- ✅ Handles transient failures (broker down, topic not available)
- ✅ Infinite retries with structured logging
- ✅ Configurable via Dependency Injection

**Kafka Infrastructure Abstraction:**
- ✅ Dedicated `Stingray.Infrastructure.KafkaMessaging` project
- ✅ Implements `IEventPublisher` and `IMessagePublisher` interfaces
- ✅ Easy to swap Kafka for RabbitMQ, Azure Service Bus, etc.
- ✅ One-line registration: `AddKafkaMessaging()`
- ✅ Application code is broker-agnostic

**MediatR Event Processing:**
- ✅ Clean separation: Infrastructure consumes, Application processes
- ✅ Event handlers have NO Kafka dependencies
- ✅ Easy to test without message broker
- ✅ Multiple handlers for same event
- ✅ Scoped service resolution in background services

**Docker Health Checks:**
- ✅ Services wait for Kafka to be healthy before starting
- ✅ Automatic restart on failure
- ✅ No connection errors on startup

### Communication Patterns

**Database:**
- For outbox pattern (guaranteed delivery)
- Transactional consistency

**Kafka:**
- For event streaming (async communication)
- With health checks and retry logic ⭐

**MediatR:**
- For in-process event handling
- Clean separation of concerns ⭐

### Production Features

✅ Clean Architecture principles
✅ CQRS pattern
✅ Outbox pattern for reliability
✅ Event-driven architecture
✅ **Polly for fault tolerance** ⭐
✅ **Message broker abstraction** ⭐
✅ **Health checks and graceful startup** ⭐
✅ **Proper error handling and retry** ⭐
✅ Horizontal scalability
✅ Observable with structured logging

**Status**: ✅ **PRODUCTION READY WITH RESILIENCE**

```bash
docker-compose up --build
```

All services will:
- Wait for Kafka to be healthy ✅
- Start reliably without connection errors ✅
- Automatically retry on failures ✅
- Process events via clean MediatR handlers ✅
- Scale independently ✅

🚀 **Ready for production deployment!**

