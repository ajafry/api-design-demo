# AI Agent Instructions — API Design Demo

> .NET 10 microservices reference project demonstrating **DDD, CQRS, Minimal API, and Azure Container Apps** deployment.
> Full docs: [README.md](README.md) · [ARCHITECTURE.md](ARCHITECTURE.md)

---

## Build & Run

```powershell
# Run both services directly (preferred for dev)
dotnet run --project src/Services/ProductService/ProductService.API --urls http://localhost:5001
dotnet run --project src/Services/OrderService/OrderService.API --urls http://localhost:5002

# Or via Docker Compose
docker compose up --build
```

- Product Service UI: http://localhost:5001/scalar/v1
- Order Service UI: http://localhost:5002/scalar/v1
- Health checks: `/health` on each service

---

## Solution Structure

```
src/
├── SharedKernel/               # Entity<TId>, IDomainEvent, Result<T> — no business logic
└── Services/
    ├── ProductService/         # "Product Catalogue" bounded context
    │   ├── .Domain             # Entities, Value Objects, Domain Events, IRepository interfaces
    │   ├── .Application        # CQRS Commands, Queries, DTOs, IService + ApplicationService
    │   ├── .Infrastructure     # InMemory repository implementations; AddProductInfrastructure()
    │   └── .API                # Minimal API endpoints, Program.cs, Dockerfile
    └── OrderService/           # "Order Management" bounded context (same structure)
```

Dependency direction: `API → Application → Domain ← Infrastructure`. Domain has **zero** external NuGet dependencies.

---

## Critical Conventions

### Layer rules (enforce always)
- **Domain**: Only `SharedKernel` reference allowed. No ASP.NET, no EF, no DTOs.
- **Application**: References Domain only. No HTTP types, no `DbContext`.
- **Infrastructure**: References Domain only. Implements its interfaces.
- **API**: References Application + Infrastructure. Wires DI; no business logic.

### Entities & Value Objects
- All entities extend `Entity<TId>` from SharedKernel (typed `Id`, domain events, identity equality).
- Value objects are `sealed record` with a private constructor and a static `Create(...)` factory that validates invariants. Never use public constructors for value objects.
- Raise domain events via `RaiseDomainEvent(...)` inside aggregate methods, not from outside.

### Result pattern
- Use `Result<T>` / `Result` for **expected failures** (validation, not found, illegal state). Never throw exceptions for these cases.
- Application service methods return `Result<T>`; endpoints map `IsSuccess` to the correct HTTP status.

### Endpoints
- Endpoints are `static` methods in a `XxxEndpoints.cs` static class with an `IEndpointRouteBuilder` extension method `MapXxxEndpoints()`.
- Always use `TypedResults` (not `Results`) for compile-time response-type correctness and automatic OpenAPI metadata.
- Route group is `api/<resource>` with `.WithTags(...)` set.
- Inject dependencies via method parameters (Minimal API parameter binding) — no `[FromServices]`.

### OpenAPI & UI
- **Do not use Swashbuckle.** Use `Microsoft.AspNetCore.OpenApi` (first-party, .NET 10).
- JSON served at `/openapi/v1.json`. Scalar UI at `/scalar/v1` via `MapScalarApiReference()`.
- Add `AddDocumentTransformer(...)` in `AddOpenApi(...)` for title/version metadata.

### Dependency Injection
- Infrastructure is registered via `Add<Service>Infrastructure()` extension methods. `Program.cs` calls only these — never registers repository implementations directly.
- Repositories are registered as **Singleton** (in-memory state must persist across requests).
- Application services are registered as **Scoped** via their interface (`IXxxService`).

### No MediatR
- Commands and Queries are plain C# `record` types. The `IXxxService` interface (one per bounded context) dispatches them directly.
- The `Commands/` and `Queries/` folders communicate *intent* — keep the separation even without a bus.

### OrderStatus serialization
- `OrderStatus` enum serializes as an **integer** in JSON (0 = Pending … 4 = Cancelled). Clients must send the integer value.

---

## Adding a New Bounded Context

Follow this checklist to mirror the existing service pattern:

1. Create four projects: `<Name>Service.Domain`, `.Application`, `.Infrastructure`, `.API`.
2. Domain references only `SharedKernel`.
3. Define aggregate root extending `Entity<Guid>`, value objects as `sealed record`, and `I<Name>Repository` interface in Domain.
4. Application: `I<Name>Service`, `<Name>ApplicationService`, Commands, Queries, DTOs.
5. Infrastructure: `InMemory<Name>Repository` as Singleton; `Add<Name>Infrastructure()` extension.
6. API: `<Name>Endpoints.cs` static class; `Program.cs` with `AddOpenApi`, health checks, ForwardedHeaders.
7. Add service to `docker-compose.yml` on a new port (5001/5002 are taken).

---

## What Is Intentionally Simple (Don't Over-Engineer)

- **In-memory repositories** — swap for EF Core + Cosmos DB / SQL Server by replacing only the Infrastructure project.
- **Domain events not dispatched** — events are accumulated in `Entity._domainEvents` but not published. Add an outbox or `IDomainEventDispatcher` when adding a message bus.
- **No authentication/authorization** — out of scope for this demo.
- **No inter-service HTTP calls** — services are fully independent at runtime.

See [ARCHITECTURE.md#8-what-is-intentionally-left-simple](ARCHITECTURE.md) for the full list and suggested next steps.
