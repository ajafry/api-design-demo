using ProductService.API.Endpoints;
using ProductService.Application.Services;
using ProductService.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ----- Services -----
builder.Services.AddOpenApi(opt =>
{
    opt.AddDocumentTransformer((doc, _, _) =>
    {
        opt.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
        doc.Info = new()
        {
            Title = "Product Service API",
            Version = "v1",
            Description = "Manages the product catalogue. Demonstrates DDD + CQRS in a microservices architecture."
        };
        return Task.CompletedTask;
    });
});

// Application services - direct injection, no dispatcher framework needed
builder.Services.AddScoped<IProductService, ProductApplicationService>();

// Infrastructure (swap InMemory for real DB without touching Domain or Application)
builder.Services.AddProductInfrastructure();

// Health checks - required for ACA liveness/readiness probes
builder.Services.AddHealthChecks();

var app = builder.Build();

// ----- Middleware pipeline -----
app.MapOpenApi();                       // serves /openapi/v1.json
app.MapScalarApiReference();            // serves /scalar/v1

app.UseHttpsRedirection();

app.MapProductEndpoints();
app.MapHealthChecks("/health");

app.Run();
