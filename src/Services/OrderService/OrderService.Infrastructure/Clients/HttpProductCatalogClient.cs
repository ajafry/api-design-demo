using System.Net.Http.Json;
using OrderService.Application.Contracts;

namespace OrderService.Infrastructure.Clients;

/// <summary>
/// Calls the Product Service REST API to look up products.
/// Registered as a typed HttpClient in <see cref="InfrastructureServiceExtensions"/>.
/// </summary>
public class HttpProductCatalogClient(HttpClient httpClient) : IProductCatalogClient
{
    public async Task<ProductLookup?> GetByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/products/{productId}", ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ProductServiceDto>(ct);
        if (dto is null) return null;

        return new ProductLookup(dto.Id, dto.Name, dto.Price, dto.Currency, dto.IsActive);
    }

    // Minimal local DTO that maps the fields we care about from the Product Service JSON response.
    // Private to this file — not exposed outside Infrastructure.
    private sealed record ProductServiceDto(
        Guid Id,
        string Name,
        decimal Price,
        string Currency,
        bool IsActive);
}
