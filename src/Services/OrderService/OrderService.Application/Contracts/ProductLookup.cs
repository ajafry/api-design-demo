namespace OrderService.Application.Contracts;

/// <summary>
/// Local representation of a product returned from the Product Catalogue service.
/// Deliberately trimmed — only the fields Order Service needs. Avoids coupling to
/// ProductService.Application.DTOs across the service boundary.
/// </summary>
public record ProductLookup(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    bool IsActive);
