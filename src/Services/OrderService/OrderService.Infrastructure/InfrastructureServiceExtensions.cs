using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Contracts;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Clients;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

        var productServiceBaseUrl = configuration["ProductService:BaseUrl"]
            ?? throw new InvalidOperationException("ProductService:BaseUrl is not configured.");

        services.AddHttpClient<IProductCatalogClient, HttpProductCatalogClient>(client =>
        {
            client.BaseAddress = new Uri(productServiceBaseUrl.TrimEnd('/') + '/');
        });

        return services;
    }
}
