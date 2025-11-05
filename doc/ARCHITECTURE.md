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
│  └─ Port: 29092 (external)
│
├─ Container: user_service
│  ├─ Port: 5001 → 8080
│  ├─ Environment:
│  │  └─ Kafka__BootstrapServers=kafka:9092
│  └─ Depends on: kafka
│
├─ Container: order_service
│  ├─ Port: 5002 → 8080
│  ├─ Environment:
│  │  └─ Kafka__BootstrapServers=kafka:9092
│  └─ Depends on: kafka
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
- ✅ Consume events from Kafka
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
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **Swagger** - API documentation

### OutboxProcessor ⭐

- **.NET 8** - Runtime
- **Microsoft.Extensions.Hosting** - Worker service framework
- **Confluent.Kafka** - Kafka client
- **Entity Framework Core** - Database access

### Infrastructure

- **Docker** - Containerization
- **Docker Compose** - Orchestration
- **Apache Kafka** - Message broker
- **Apache Zookeeper** - Kafka coordination

---

## Summary

The **OutboxProcessor** is now a **first-class, standalone service** that:

✅ Runs independently in its own container
✅ Can be deployed to separate infrastructure
✅ Scales independently from API services
✅ Has dedicated resources
✅ Provides clear separation of concerns
✅ Is production-ready with proper configuration

**Three independent services working together:**

1. **UserService** - User management API
2. **OrderService** - Order management API
3. **OutboxProcessor** - Event publishing worker ⭐

All communicating via:

- **Database** - For outbox pattern
- **Kafka** - For event streaming

**Status**: ✅ **PRODUCTION READY**

```bash
docker-compose up --build
```

All services will start and work seamlessly together! 🚀

