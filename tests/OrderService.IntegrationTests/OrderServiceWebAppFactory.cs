using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using OrderService.Application.Contracts;

namespace OrderService.IntegrationTests;

/// <summary>
/// Boots OrderService in-process. Replaces IProductCatalogClient with an NSubstitute
/// mock so tests control catalog responses without a running ProductService.
/// </summary>
public class OrderServiceWebAppFactory : WebApplicationFactory<Program>
{
    public IProductCatalogClient CatalogClient { get; } = Substitute.For<IProductCatalogClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a dummy URL so AddOrderInfrastructure() doesn't throw on startup.
        // The real HttpProductCatalogClient is never used — it's replaced below.
        builder.UseSetting("ProductService:BaseUrl", "http://test-placeholder");

        builder.ConfigureServices(services =>
        {
            // Remove the typed HttpClient registration added by AddOrderInfrastructure()
            services.RemoveAll<IProductCatalogClient>();

            // Register our NSubstitute mock so every request uses it
            services.AddSingleton(CatalogClient);
        });
    }
}
