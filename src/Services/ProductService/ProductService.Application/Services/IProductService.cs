using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using SharedKernel;

namespace ProductService.Application.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(CreateProductCommand command, CancellationToken ct = default);
    Task<Result> UpdateAsync(UpdateProductCommand command, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
