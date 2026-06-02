using ProductService.Application.DTOs;

namespace ProductService.Application.Queries;

/// <summary>
/// Queries are read-side operations in CQRS - they do not change state.
/// </summary>
public record GetAllProductsQuery();

public record GetProductByIdQuery(Guid Id);

public record GetProductsByCategoryQuery(string Category);
