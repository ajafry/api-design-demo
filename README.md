# API Design (Microservices) Demo

A .NET 10 microservices reference project demonstrating **DDD**, **CQRS**, **containerisation**, and deployment patterns for **Azure Container Apps (ACA)** and **Azure API Management (APIM)**.

---

## Architecture

```
ApiDesignDemo/
├── src/
│   ├── SharedKernel/               # Domain primitives shared across services
│   │   ├── Entity<TId>             # Base entity with domain event support
│   │   ├── IDomainEvent            # Marker interface for domain events
│   │   └── Result<T>               # Railway-oriented error handling
│   │
│   └── Services/
│       ├── ProductService/         # Product catalogue bounded context
│       │   ├── Domain/             # Entities, Value Objects, Domain Events, Repo interfaces
│       │   ├── Application/        # CQRS Commands, Queries, Application Services
│       │   ├── Infrastructure/     # In-memory repository (swap for EF/Cosmos DB)
│       │   └── API/                # Minimal API endpoints + Scalar UI + Dockerfile
│       │
│       └── OrderService/           # Orders bounded context
│           ├── Domain/
│           ├── Application/
│           ├── Infrastructure/
│           └── API/
└── docker-compose.yml
```

### Dependency Direction (DDD)
```
API → Application → Domain ← Infrastructure
                  ↑
             SharedKernel
```
The Domain layer has **zero** external dependencies — it owns the business rules.

---

## Key Patterns Demonstrated

| Pattern | Where |
|---------|-------|
| **Domain Entities** with identity | `Product`, `Order` aggregate roots |
| **Value Objects** (structural equality) | `Money`, `OrderItem` |
| **Domain Events** | `ProductCreatedEvent`, `OrderStatusChangedEvent` |
| **Repository abstraction** | `IProductRepository`, `IOrderRepository` in Domain |
| **CQRS** (command/query separation) | `Commands/`, `Queries/`, `Services/` in Application |
| **Result pattern** | `Result<T>` — no exception-driven flow for expected failures |
| **State machine** | `Order.UpdateStatus()` enforces valid transitions |
| **Health checks** | `/health` endpoint on every service (ACA probe ready) |
| **OpenAPI / Scalar UI** | `/scalar/v1` on every service (APIM import ready) |

---

## Running Locally

### Direct (dotnet run)
```bash
# Terminal 1 – Product Service on :5001
dotnet run --project src/Services/ProductService/ProductService.API --urls http://localhost:5001

# Terminal 2 – Order Service on :5002
dotnet run --project src/Services/OrderService/OrderService.API --urls http://localhost:5002
```

Then open:
- http://localhost:5001/scalar/v1
- http://localhost:5002/scalar/v1

### Docker Compose
```bash
docker compose up --build
```
- Product Service → http://localhost:5001/scalar/v1
- Order Service   → http://localhost:5002/scalar/v1

### Smoke-testing with curl

**Product Service**
```bash
# List all seeded products
curl -s http://localhost:5001/api/products | jq .

# Get a single product (copy an id from the list above)
curl -s http://localhost:5001/api/products/<id> | jq .

# Filter by category
curl -s http://localhost:5001/api/products/category/Electronics | jq .

# Create a new product
curl -s -X POST http://localhost:5001/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mechanical Keyboard",
    "description": "Tactile switches, TKL layout",
    "price": 129.99,
    "currency": "GBP",
    "stockQuantity": 50,
    "category": "Electronics"
  }' | jq .

# Update a product (use the id returned from the create above)
curl -s -X PUT http://localhost:5001/api/products/<id> \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mechanical Keyboard Pro",
    "description": "Tactile switches, full layout",
    "price": 149.99,
    "currency": "GBP",
    "stockQuantity": 30,
    "category": "Electronics"
  }'

# Soft-delete a product
curl -s -X DELETE http://localhost:5001/api/products/<id>

# Health check
curl -s http://localhost:5001/health
```

**Order Service**
```bash
# List all seeded orders
curl -s http://localhost:5002/api/orders | jq .

# Get a single order (copy an id from the list above)
curl -s http://localhost:5002/api/orders/<id> | jq .

# Get orders for a customer
curl -s http://localhost:5002/api/orders/customer/CUST-001 | jq .

# Create a new order
curl -s -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-042",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "productName": "Widget A",
        "quantity": 2,
        "unitPrice": 49.99,
        "currency": "GBP"
      }
    ]
  }' | jq .

# Advance order status (Pending → Confirmed)
curl -s -X PATCH http://localhost:5002/api/orders/<id>/status \
  -H "Content-Type: application/json" \
  -d '{ "status": 1 }'

# Try an invalid transition to see the state machine reject it (Pending → Delivered)
curl -s -X PATCH http://localhost:5002/api/orders/<id>/status \
  -H "Content-Type: application/json" \
  -d '{ "status": 3 }'

# Health check
curl -s http://localhost:5002/health
```

> **Order status values:** `0` = Pending, `1` = Confirmed, `2` = Shipped, `3` = Delivered, `4` = Cancelled.  
> `jq` is optional — omit `| jq .` if it isn't installed.

---

## APIs

### Product Service (`/api/products`)
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/products` | List all active products |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/category/{category}` | Filter by category |
| POST | `/api/products` | Create a product |
| PUT | `/api/products/{id}` | Update a product |
| DELETE | `/api/products/{id}` | Soft-delete (deactivate) |

### Order Service (`/api/orders`)
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/orders` | List all orders |
| GET | `/api/orders/{id}` | Get order by ID |
| GET | `/api/orders/customer/{customerId}` | Orders for a customer |
| POST | `/api/orders` | Create an order |
| PATCH | `/api/orders/{id}/status` | Transition order status |

**Order status state machine:** `Pending → Confirmed → Shipped → Delivered`  
(or `Pending/Confirmed → Cancelled`)

---

## Deploying to Azure Container Apps (ACA)

1. **Push images to ACR**
   ```bash
   az acr build --registry <acr-name> --image product-service:v1 \
     --file src/Services/ProductService/ProductService.API/Dockerfile .

   az acr build --registry <acr-name> --image order-service:v1 \
     --file src/Services/OrderService/OrderService.API/Dockerfile .
   ```

2. **Create Container Apps**
   ```bash
   az containerapp create \
     --name product-service \
     --resource-group <rg> \
     --environment <aca-env> \
     --image <acr-name>.azurecr.io/product-service:v1 \
     --target-port 8080 \
     --ingress external \
     --min-replicas 1 --max-replicas 5
   ```
   Repeat for `order-service`.

3. **Health probes** — ACA automatically uses `/health` for liveness/readiness.

---

## Importing to APIM

Each service exposes OpenAPI at `/openapi/v1.json`. Import via:

```bash
az apim api import \
  --resource-group <rg> \
  --service-name <apim-name> \
  --path products \
  --specification-url https://<product-service-fqdn>/openapi/v1.json \
  --specification-format OpenApi
```

---

## Swapping the In-Memory Store for a Real Database

The infrastructure layer is isolated behind the `IProductRepository` / `IOrderRepository` interfaces. To use a real database:

1. Add an EF Core / Cosmos DB implementation in the `Infrastructure` project.
2. Update `InfrastructureServiceExtensions.cs` to register the new implementation.
3. **Zero changes** required in the `Domain` or `Application` layers.
