namespace OrderService.Application.Contracts;

/// <summary>
/// Port that Order Service uses to query the Product Catalogue.
/// The interface lives in Application so the use-case logic stays independent of HTTP.
/// The concrete implementation (HttpProductCatalogClient) lives in Infrastructure.
/// </summary>
public interface IProductCatalogClient
{
    /// <summary>
    /// Returns the product with the given ID, or <c>null</c> if it does not exist.
    /// </summary>
    Task<ProductLookup?> GetByIdAsync(Guid productId, CancellationToken ct = default);
}
