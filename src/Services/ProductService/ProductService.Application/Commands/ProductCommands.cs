using SharedKernel;

namespace ProductService.Application.Commands;

/// <summary>
/// Commands are write-side operations in CQRS - they change state and return a Result.
/// </summary>
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    string Category
);

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    string Category
);

public record DeleteProductCommand(Guid Id);
