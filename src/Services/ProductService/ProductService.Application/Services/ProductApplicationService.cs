using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using ProductService.Domain.Repositories;
using SharedKernel;

namespace ProductService.Application.Services;

public class ProductApplicationService(IProductRepository repository) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await repository.GetAllAsync(ct);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(id, ct);
        return product is null ? null : ToDto(product);
    }

    public async Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var products = await repository.GetByCategoryAsync(category, ct);
        return products.Select(ToDto).ToList();
    }

    public async Task<Result<Guid>> CreateAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var product = Product.Create(
            command.Name, command.Description,
            command.Price, command.Currency,
            command.StockQuantity, command.Category);

        await repository.AddAsync(product, ct);
        return Result<Guid>.Success(product.Id);
    }

    public async Task<Result> UpdateAsync(UpdateProductCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(command.Id, ct);
        if (product is null) return Result.Failure($"Product {command.Id} not found.");

        product.Update(command.Name, command.Description,
            command.Price, command.Currency,
            command.StockQuantity, command.Category);

        await repository.UpdateAsync(product, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(id, ct);
        if (product is null) return Result.Failure($"Product {id} not found.");

        await repository.DeleteAsync(id, ct);
        return Result.Success();
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Description,
        p.Price.Amount, p.Price.Currency,
        p.StockQuantity, p.Category, p.IsActive,
        p.CreatedAt, p.UpdatedAt);
}
