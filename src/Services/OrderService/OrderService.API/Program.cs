using OrderService.Application.Services;
using OrderService.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ----- Services -----
builder.Services.AddControllers();
builder.Services.AddOpenApi(opt =>
{
    opt.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new()
        {
            Title = "Order Service API",
            Version = "v1",
            Description = "Manages customer orders with state-machine-driven status transitions. Demonstrates DDD + CQRS in a microservices architecture."
        };
        return Task.CompletedTask;
    });
});

// Application services - direct injection, no dispatcher framework needed
builder.Services.AddScoped<IOrderService, OrderApplicationService>();

// Infrastructure
builder.Services.AddOrderInfrastructure();

// Health checks - required for ACA liveness/readiness probes
builder.Services.AddHealthChecks();

var app = builder.Build();

// ----- Middleware pipeline -----
app.MapOpenApi();                       // serves /openapi/v1.json
app.MapScalarApiReference();            // serves /scalar/v1

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
