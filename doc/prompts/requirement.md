You are ChatGPT, an expert .NET microservices engineer. Enhance and implement a **containerized, event-driven e-commerce backend** using **.NET 8**, **Entity Framework Core**, **Kafka**, and **Docker Compose**, following an existing **Clean Architecture** solution structure.

## 🧱 Existing Solution Overview

The solution, named **Stingray**, already follows Clean Architecture conventions:

```
Stingray
│
├── Services
│   ├── Stingray.Services.OrderService
│   └── Stingray.Services.UserService
│
├── Storage
│   └── Stingray.Storage.InMemory
│
├── Stingray.Application
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   ├── Validators/
│   └── Dependencies/
│
└── Stingray.Domain
    ├── Events/
    ├── Outbox/
    ├── Dependencies/
    └── Entities (e.g., User.cs)
```

Maintain this structure and extend functionality within the appropriate layers.

* All the core entitieds + repository interfaces should be defined in `Stingray.Domain`.
* Business logic should reside in `Stingray.Application.Services` please use MediatR with CQRS handlers.
* Data persistence should be handled in `Stingray.Storage.InMemory` using EF Core's in-memory database and should implement repository interfaces in `Stingray.Domain` .
* API endpoints and event cosumers should be defined in the respective service projects under `Stingray.Services.*`. Use minimal APIS for defining endpoints.

## 🎯 Goal

Build a microservices-based **e-commerce backend** demonstrating:

* Event-driven architecture using Kafka
* Cleanly separated microservices (User and Order)
* In-memory persistence (EF Core)
* Containerized environment (Docker Compose)

## 🧩 Services

### User Service

* Handles user registration and lookup
* Endpoints:

    * `POST /users` → Create user
    * `GET /users/{id}` → Get user by ID
* Use **EF Core (in-memory)** for persistence
* Use minimal apis for the enpoints
* Publish a **UserCreated** event to Kafka after creation
* Apply validation via the `Validators` layer
* Use `Application.Services` for business logic and `Application.DTOs` for request/response models

### Order Service

* Handles order creation and retrieval
* Endpoints:

    * `POST /orders` → Create order
    * `GET /orders/{id}` → Get order by ID
* Use **EF Core (in-memory)**
* Use minimal apis for the enpoints
* Publish an **OrderCreated** event to Kafka
* Consume **UserCreated** events for cross-service communication
* Use domain models and event classes in `Stingray.Domain.Events`

## ⚙️ Event-Driven Communication

* Define Kafka producers/consumers under `Application.Interfaces`
* Use `Stingray.Domain.Events` for shared event definitions
* Implement an **outbox pattern** (in `Stingray.Domain.Outbox`) to ensure reliable event publishing
* Demonstrate event-driven flow between services (User → Order)

## 🧩 Infrastructure

* Each service has its own **Dockerfile**
* Create a **docker-compose.yml** to orchestrate:

    * User Service
    * Order Service
    * Kafka + Zookeeper
* Ensure networking and connectivity between services
* Provide Swagger documentation for both APIs

Focus on producing a **clean, event-driven microservices implementation** that leverages the existing Clean Architecture structure while being fully runnable via Docker.
