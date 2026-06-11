using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Resources;
using ProductService.API.Endpoints;
using ProductService.Application.Services;
using ProductService.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ----- Observability -----
// Sends ILogger output, ASP.NET request telemetry, dependencies, and exceptions
// to Application Insights. Only registered when a connection string is supplied
// (set APPLICATIONINSIGHTS_CONNECTION_STRING as a Container App env var/secret
// in production). Skipped in local dev and integration tests where the SDK
// would otherwise throw on missing configuration.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("product-service"))
        .UseAzureMonitor();
}

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
// Trust X-Forwarded-Proto from ACA ingress so the OpenAPI spec reports https.
// KnownNetworks/KnownProxies are cleared because ACA's ingress proxy is not
// on a loopback address, so headers would otherwise be silently ignored.
var forwardedOptions = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto };
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

app.MapOpenApi();                       // serves /openapi/v1.json
app.MapScalarApiReference();            // serves /scalar/v1

app.UseHttpsRedirection();

app.MapProductEndpoints();
app.MapHealthChecks("/health");

app.Run();
