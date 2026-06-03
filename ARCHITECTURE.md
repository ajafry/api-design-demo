# Architecture Guide

A deep-dive into the design decisions, project structure, and patterns used in the API Design Microservices Demo.

---

## Table of Contents

1. [Solution Overview](#1-solution-overview)
2. [Bounded Contexts](#2-bounded-contexts)
3. [DDD Layer Model](#3-ddd-layer-model)
4. [Project-by-Project Reference](#4-project-by-project-reference)
   - [SharedKernel](#sharedkernel)
   - [ProductService.Domain](#productservicedomain)
   - [ProductService.Application](#productserviceapplication)
   - [ProductService.Infrastructure](#productserviceinfrastructure)
   - [ProductService.API](#productserviceapi)
   - [OrderService.Domain](#orderservicedomain)
   - [OrderService.Application](#orderserviceapplication)
   - [OrderService.Infrastructure](#orderserviceinfrastructure)
   - [OrderService.API](#orderserviceapi)
5. [Key Patterns Explained](#5-key-patterns-explained)
   - [Domain Entities & Aggregate Roots](#51-domain-entities--aggregate-roots)
   - [Value Objects](#52-value-objects)
   - [Domain Events](#53-domain-events)
   - [Repository Pattern](#54-repository-pattern)
   - [CQRS (without a bus)](#55-cqrs-without-a-bus)
   - [Result Pattern](#56-result-pattern)
   - [Order State Machine](#57-order-state-machine)
6. [Dependency Rules](#6-dependency-rules)
7. [API & OpenAPI](#7-api--openapi)
8. [What Is Intentionally Left Simple](#8-what-is-intentionally-left-simple)
9. [Suggested Next Steps](#9-suggested-next-steps)

---

## 1. Solution Overview

```
ApiDesignDemo.slnx
└── src/
    ├── SharedKernel/                   # Domain primitives — no service-specific knowledge
    └── Services/
        ├── ProductService/             # "Product Catalogue" bounded context
        │   ├── ProductService.Domain
        │   ├── ProductService.Application
        │   ├── ProductService.Infrastructure
        │   └── ProductService.API
        └── OrderService/               # "Order Management" bounded context
            ├── OrderService.Domain
            ├── OrderService.Application
            ├── OrderService.Infrastructure
            └── OrderService.API
```

Each bounded context is a self-contained vertical slice: it owns its own entities, business rules, persistence, and HTTP surface. The two services share **nothing at runtime** — they are connected only by the compile-time `SharedKernel` library.

---

## 2. Bounded Contexts

| Context | Responsibility | Aggregate Root | Key Invariants |
|---------|---------------|----------------|----------------|
| **Product Catalogue** | Manage the catalogue of available products, their pricing, stock, and active/inactive state | `Product` | Price must be ≥ 0; currency is required; soft-delete (never hard-delete) |
| **Order Management** | Create customer orders, track their fulfilment lifecycle | `Order` | Order must have at least one item; status can only move forward along a defined path |

In a production system these would run as separate deployable units (containers, ACA apps) with their own databases. The demo wires them up in one solution for convenience but maintains the same hard boundaries.

---

## 3. DDD Layer Model

Each service follows the same four-layer DDD structure:

```
┌──────────────────────────────────────────────────────┐
│  API  (HTTP surface, Minimal API endpoints, OpenAPI, DI wiring) │
│  depends on Application + Infrastructure                        │
├──────────────────────────────────────────────────────┤
│  Application  (use-cases, CQRS commands/queries,      │
│               DTOs, service interfaces)               │
│  depends on Domain only                               │
├──────────────────────────────────────────────────────┤
│  Infrastructure  (repository implementations,         │
│                  EF/Cosmos/in-memory adapters)        │
│  depends on Domain only                               │
├──────────────────────────────────────────────────────┤
│  Domain  (entities, value objects, domain events,     │
│           repository interfaces)                      │
│  NO external dependencies                             │
└──────────────────────────────────────────────────────┘
                         ↑
                    SharedKernel
          (referenced by Domain of every service)
```

**The golden rule:** every arrow points inward. Nothing in Domain knows about HTTP, JSON, databases, or ASP.NET. This means you can swap the web framework, the database, or the transport layer and the business logic doesn't change.

---

## 4. Project-by-Project Reference

### SharedKernel

> `src/SharedKernel/`

A class library referenced by every service's Domain layer. It contains only primitives that are universally useful — there is no business logic here.

| File | Purpose |
|------|---------|
| `Entity<TId>` | Abstract base class for aggregate roots. Provides a typed `Id`, domain event collection (`RaiseDomainEvent`, `ClearDomainEvents`), and value equality based on `Id`. |
| `IDomainEvent` | Marker interface. Any record that implements this can be raised by an entity and later dispatched (e.g. to a message bus). |
| `Result<T>` / `Result` | Railway-oriented return type. Wraps either a success value or an error string. Replaces throwing exceptions for expected failures (e.g. "product not found"). |

---

### ProductService.Domain

> `src/Services/ProductService/ProductService.Domain/`

The heart of the Product Catalogue context. Contains all business rules. Zero NuGet dependencies.

```
Domain/
├── Entities/
│   └── Product.cs          # The aggregate root
├── ValueObjects/
│   └── Money.cs            # Pricing value object
├── Events/
│   └── ProductEvents.cs    # Domain events raised by Product
└── Repositories/
    └── IProductRepository.cs  # Persistence contract (interface only)
```

**`Product` aggregate root**  
Owns all product data and enforces invariants through factory methods and private setters. Consumers cannot set `Name` or `Price` directly — they must call `Create(...)` or `Update(...)`, which also raise domain events.

```
Product.Create(...)     → raises ProductCreatedEvent
Product.Update(...)     → raises ProductUpdatedEvent
Product.Deactivate()    → soft-deletes (IsActive = false), no event raised
```

**`Money` value object**  
Represents price as an amount + currency pair. Implemented as a `sealed record` for structural equality (two `Money` instances with the same amount and currency are equal). The private constructor and `Create(...)` factory enforce that amount ≥ 0 and currency is non-empty.

**`IProductRepository`**  
An interface in the Domain layer. The Domain declares *what* it needs from persistence (`GetByIdAsync`, `GetAllAsync`, etc.) without knowing *how* it is stored. The concrete implementation lives in Infrastructure.

---

### ProductService.Application

> `src/Services/ProductService/ProductService.Application/`

Orchestrates use-cases. Knows about the Domain but nothing about HTTP or databases.

```
Application/
├── Commands/
│   └── ProductCommands.cs   # CreateProductCommand, UpdateProductCommand, DeleteProductCommand
├── Queries/
│   └── ProductQueries.cs    # GetAllProductsQuery, GetProductByIdQuery, GetProductsByCategoryQuery
├── DTOs/
│   └── ProductDto.cs        # Read model returned to callers (no domain objects cross this boundary)
├── Services/
│   ├── IProductService.cs          # Public interface for the application layer
│   └── ProductApplicationService.cs # Single class that implements all use-cases
└── Handlers/
    └── CommandHandlers.cs   # Stub — logic consolidated into ProductApplicationService
    └── QueryHandlers.cs     # Stub — logic consolidated into ProductApplicationService
```

**Commands and Queries** are plain C# records with no framework marker interfaces. They carry only the data needed for a use-case — nothing more.

**`IProductService`** is the public API of the Application layer. The endpoint handlers depend on this interface, not on any implementation. This makes the application layer unit-testable in isolation.

**`ProductApplicationService`** implements all six use-cases: `GetAllAsync`, `GetByIdAsync`, `GetByCategoryAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`. It translates between the HTTP-facing `Command`/`Query` records and the Domain's aggregate root, then maps the result to a `ProductDto` before returning.

> **Why no MediatR?**  
> MediatR is a popular library that adds an in-process bus between controllers and handlers. For a two-service demo it adds indirection without benefit — a simple interface achieves the same decoupling with less ceremony. The `Commands/` and `Queries/` folders remain because the separation of intent (a command mutates state; a query reads it) is a valuable discipline regardless of dispatch mechanism.

---

### ProductService.Infrastructure

> `src/Services/ProductService/ProductService.Infrastructure/`

Adapts the Domain's repository interface to a concrete storage technology. This is the only layer allowed to know about databases, ORMs, or other I/O.

```
Infrastructure/
├── Repositories/
│   └── InMemoryProductRepository.cs  # ConcurrentDictionary-backed implementation
└── InfrastructureServiceExtensions.cs # AddProductInfrastructure() extension method
```

**`InMemoryProductRepository`** stores products in a `ConcurrentDictionary<Guid, Product>` and seeds three demo products on startup. It is registered as a **Singleton** so that state persists across HTTP requests.

**`AddProductInfrastructure()`** is a DI extension method that hides the registration details. `Program.cs` calls `builder.Services.AddProductInfrastructure()` and knows nothing about which implementation is in use. To swap in a real database, replace only this file.

---

### ProductService.API

> `src/Services/ProductService/ProductService.API/`

The HTTP host. Responsible for routing, serialisation, OpenAPI documentation, and wiring up the DI container.

```
API/
├── Endpoints/
│   └── ProductEndpoints.cs     # 6 endpoints as static methods, registered via MapProductEndpoints()
├── Program.cs                  # DI + middleware pipeline
└── Dockerfile                  # Multi-stage build → non-root ASP.NET runtime image
```

**`ProductEndpoints`** is a static class with an `IEndpointRouteBuilder` extension method `MapProductEndpoints()`. Each endpoint is a private static async method injected via parameter binding — no controller class, no `[ApiController]`, no `[Route]` attributes. `TypedResults` (not `Results`) is used throughout for compile-time response-type correctness and automatic OpenAPI metadata flow.

**`Program.cs`** registers:
- `AddOpenApi(...)` with a document transformer for title/version metadata
- `AddScoped<IProductService, ProductApplicationService>()`
- `AddProductInfrastructure()` (which adds the repository singleton)
- `AddHealthChecks()` for the ACA `/health` probe

And maps endpoints with `app.MapProductEndpoints()` — there is no `AddControllers()`, `UseAuthorization()`, or `MapControllers()` in the pipeline.

**OpenAPI**: served at `/openapi/v1.json`. The Scalar interactive UI is served at `/scalar/v1`. No Swashbuckle — `Microsoft.AspNetCore.OpenApi` is the first-party package included with .NET 10.

---

### OrderService.Domain

> `src/Services/OrderService/OrderService.Domain/`

Mirrors the structure of ProductService.Domain but models a different bounded context.

```
Domain/
├── Entities/
│   └── Order.cs              # Aggregate root — owns OrderItems
├── ValueObjects/
│   └── OrderItem.cs          # Line-item value object (product + qty + price)
├── Enums/
│   └── OrderStatus.cs        # Pending | Confirmed | Shipped | Delivered | Cancelled
├── Events/
│   └── OrderEvents.cs        # OrderCreatedEvent, OrderStatusChangedEvent
└── Repositories/
    └── IOrderRepository.cs
```

**`Order` aggregate root**  
Contains a private `List<OrderItem>` and exposes it as `IReadOnlyList<OrderItem>`. The only way to modify the collection is via the `Create(...)` factory — this is the *encapsulation of invariants* that DDD requires. `TotalAmount` is computed on the fly from the line items.

**`OrderItem` value object**  
A `sealed record` with a computed `LineTotal` property (`UnitPrice * Quantity`). Two `OrderItem` instances with identical fields are structurally equal.

**`OrderStatus` enum**  
Defines the five lifecycle states. The valid transitions are enforced by `Order.UpdateStatus()`, not by the enum itself.

---

### OrderService.Application

> `src/Services/OrderService/OrderService.Application/`

```
Application/
├── Commands/
│   └── OrderCommands.cs    # CreateOrderCommand, UpdateOrderStatusCommand, OrderItemRequest
├── Queries/
│   └── OrderQueries.cs     # GetAllOrdersQuery, GetOrderByIdQuery, GetOrdersByCustomerQuery
├── DTOs/
│   └── OrderDto.cs         # Flattened read model (OrderItemDto nested inside)
└── Services/
    ├── IOrderService.cs
    └── OrderApplicationService.cs
```

**`IOrderService`** has five methods. `UpdateStatusAsync` accepts an `UpdateOrderStatusCommand` which carries the target `OrderStatus`. The application service translates this into a call to `order.UpdateStatus(...)` on the aggregate, catching any `InvalidOperationException` raised by an illegal transition and wrapping it in a `Result.Failure(...)`.

---

### OrderService.Infrastructure

> `src/Services/OrderService/OrderService.Infrastructure/`

Identical in structure to the Product infrastructure. Seeds one demo order for `customer-001` with two line items on startup.

---

### OrderService.API

> `src/Services/OrderService/OrderService.API/`

```
API/
├── Endpoints/
│   └── OrderEndpoints.cs     # 5 endpoints as static methods, registered via MapOrderEndpoints()
├── Program.cs
└── Dockerfile
```

The `PATCH` endpoint accepts a `UpdateStatusRequest(OrderStatus Status)` body. The `OrderStatus` enum serialises as an integer in JSON (`0`–`4`). The status-update handler routes the error response intelligently: if the error message contains "not found" it returns `404 NotFound<string>`, otherwise `400 BadRequest<string>`.

---

## 5. Key Patterns Explained

### 5.1 Domain Entities & Aggregate Roots

An **entity** has an identity (`Id`) that is stable across time. Two `Product` objects with the same `Id` represent the same product even if their properties differ (e.g. different prices at two points in time).

An **aggregate root** is an entity that forms the consistency boundary for a cluster of related objects. `Order` is the root of the `Order`/`OrderItem` cluster — you never modify an `OrderItem` directly, you go through `Order`.

All entities in this solution extend `Entity<TId>` from SharedKernel, which gives them:
- A typed `Id` property
- A domain event list
- Identity-based `Equals` / `GetHashCode`

### 5.2 Value Objects

A **value object** has no identity — it is defined entirely by its properties. `Money(100.00, "GBP")` equals any other `Money(100.00, "GBP")`.

In this codebase value objects are implemented as `sealed record` types. C# records provide:
- Structural equality (`==`, `Equals`, `GetHashCode`) for free
- Immutability by default (init-only properties or read-only fields)
- Concise syntax

The private constructor + static `Create(...)` factory pattern enforces validation at construction time, making it **impossible to create an invalid value object**.

### 5.3 Domain Events

Domain events record that something meaningful happened inside a bounded context. They are raised by aggregate roots (via `RaiseDomainEvent(...)`) and accumulated in the entity's `_domainEvents` list.

In this demo they are collected but not dispatched — there is no event bus wired up. In production you would dispatch them after saving to the database (outbox pattern, or inline via a `DomainEventDispatcher` in your unit-of-work).

| Event | Raised by | Payload |
|-------|-----------|---------|
| `ProductCreatedEvent` | `Product.Create(...)` | ProductId, Name, Price |
| `ProductUpdatedEvent` | `Product.Update(...)` | ProductId, Name |
| `ProductDeletedEvent` | (available, not raised yet) | ProductId |
| `OrderCreatedEvent` | `Order.Create(...)` | OrderId, CustomerId, TotalAmount |
| `OrderStatusChangedEvent` | `Order.UpdateStatus(...)` | OrderId, NewStatus |

### 5.4 Repository Pattern

The **repository** abstracts persistence behind an interface that speaks the Domain's language — it takes and returns domain objects, not DTOs or raw SQL rows.

```
Domain layer defines:     IProductRepository  (just an interface)
Infrastructure implements: InMemoryProductRepository
```

The DI container binds them together at startup. The Domain and Application layers never see the `InMemory` implementation — they only know the interface. This is the **Dependency Inversion Principle** in action.

### 5.5 CQRS (without a bus)

**CQRS** (Command Query Responsibility Segregation) separates operations that mutate state (commands) from operations that read state (queries). The key benefits:

- Commands return `Result<Guid>` or `Result` — they either succeed or fail, they don't return data
- Queries return DTOs — they never touch the write model
- Each direction can be optimised independently (e.g. queries can hit a read replica or a denormalised projection)

In this demo, commands and queries are plain C# records dispatched directly to the application service — no in-process bus is needed. The folder structure (`Commands/`, `Queries/`, `Services/`) makes the intent visible.

### 5.6 Result Pattern

Instead of throwing exceptions for expected failures (e.g. "product not found", "invalid status transition"), the application service returns a `Result` or `Result<T>`:

```csharp
// Result<T> — carries a value on success
Result<Guid>.Success(product.Id)
Result<Guid>.Failure("Product not found.")

// Result — success/failure only
Result.Success()
Result.Failure("Cannot transition from Shipped to Pending.")
```

The endpoint handler checks `result.IsSuccess` and maps to the appropriate HTTP status code (`200`, `404`, `400`). This keeps the application layer framework-agnostic — it returns data and errors, not `IActionResult`.

### 5.7 Order State Machine

`Order.UpdateStatus()` encodes the business rules for order progression as a switch expression:

```
Pending    → Confirmed ✓
Pending    → Cancelled ✓
Confirmed  → Shipped   ✓
Confirmed  → Cancelled ✓
Shipped    → Delivered ✓
anything else          ✗  (throws InvalidOperationException)
```

The rules live entirely in the Domain layer. The endpoint handler and application service have no knowledge of which transitions are valid.

---

## 6. Dependency Rules

| Layer | May depend on | Must NOT depend on |
|-------|--------------|-------------------|
| Domain | SharedKernel only | Application, Infrastructure, API, ASP.NET, EF Core |
| Application | Domain, SharedKernel | Infrastructure, API, ASP.NET, EF Core |
| Infrastructure | Domain, SharedKernel | Application, API, ASP.NET |
| API | Application, Infrastructure, Domain | (no restriction, but avoid bypassing Application) |
| SharedKernel | Nothing | Everything else |

These rules are enforced by the `.csproj` `<ProjectReference>` entries — there are no circular references.

---

## 7. API & OpenAPI

Both services use **`Microsoft.AspNetCore.OpenApi`** (the first-party .NET 10 package) instead of Swashbuckle. The spec is generated at runtime and served at:

| URL | Content |
|-----|---------|
| `/openapi/v1.json` | Raw OpenAPI 3.x JSON spec (import into APIM) |
| `/scalar/v1` | Scalar interactive API explorer UI |
| `/health` | ASP.NET health check endpoint (ACA liveness/readiness probe) |

**Why Scalar instead of Swagger UI?**  
Scalar is a modern, actively maintained UI for OpenAPI specs with a cleaner interface and better support for OpenAPI 3.1 features.

---

## 8. What Is Intentionally Left Simple

This is a reference project, not a production system. The following shortcuts are deliberate:

| Simplification | Production equivalent |
|---------------|----------------------|
| In-memory `ConcurrentDictionary` repositories | EF Core with SQL Server, or Azure Cosmos DB |
| No authentication / authorisation | Azure AD / Entra ID with APIM OAuth2 policy |
| Domain events collected but not dispatched | Outbox pattern → Azure Service Bus / Event Grid |
| `FluentValidation` added as a dependency but not wired up | Validation pipeline in Application layer |
| Single `Program.cs` DI file | Modular feature registration, options pattern |
| No integration or unit tests | xUnit + Testcontainers for integration, Moq for unit |
| No distributed tracing | OpenTelemetry → Azure Monitor / Application Insights |
| Both services share a solution | Separate repos / monorepo with independent CI pipelines |

---

## 9. Suggested Next Steps

### Replace the in-memory store
Add an EF Core or Azure Cosmos DB implementation in the `Infrastructure` project. Register it in `InfrastructureServiceExtensions.cs`. Zero changes to Domain or Application.

### Wire up FluentValidation
`FluentValidation` is already referenced in the Application `.csproj` files. Add validators for each command and call them at the start of each application service method.

### Dispatch domain events
After saving to the database, iterate `entity.DomainEvents`, publish them to Azure Service Bus, then call `entity.ClearDomainEvents()`.

### Add an API Gateway
Import `/openapi/v1.json` into Azure API Management. Add rate-limiting, JWT validation, and subscription-key policies at the gateway layer.

### Add observability
Add `OpenTelemetry.Extensions.Hosting` and export traces + metrics to Azure Monitor / Application Insights.

### Deploy to Azure Container Apps
See the [README](README.md) for `az containerapp create` commands. Each service has a `Dockerfile` ready to push to Azure Container Registry.
