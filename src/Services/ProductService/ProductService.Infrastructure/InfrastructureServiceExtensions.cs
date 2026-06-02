using Microsoft.Extensions.DependencyInjection;
using ProductService.Domain.Repositories;
using ProductService.Infrastructure.Repositories;

namespace ProductService.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddProductInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        return services;
    }
}
