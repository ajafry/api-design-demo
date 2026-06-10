using Microsoft.AspNetCore.HttpOverrides;
using OrderService.API.Endpoints;
using OrderService.Application.Services;
using OrderService.Infrastructure;
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
builder.Services.AddOrderInfrastructure(builder.Configuration);

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

app.MapOrderEndpoints();
app.MapHealthChecks("/health");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
