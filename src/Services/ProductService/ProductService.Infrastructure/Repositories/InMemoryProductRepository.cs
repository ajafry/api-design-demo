using ProductService.Domain.Entities;
using ProductService.Domain.Repositories;
using System.Collections.Concurrent;

namespace ProductService.Infrastructure.Repositories;

/// <summary>
/// In-memory repository implementation. Swap this for a real database (EF Core / Cosmos DB) with zero
/// changes to the Domain or Application layers — that's the power of the Repository pattern in DDD.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _store = [];

    public InMemoryProductRepository()
    {
        // Seed with demo data
        var seed = new[]
        {
            Product.Create("Widget Pro", "Professional grade widget", 29.99m, "GBP", 100, "Widgets"),
            Product.Create("Gadget Lite", "Entry level gadget for everyday use", 9.99m, "GBP", 250, "Gadgets"),
            Product.Create("Super Gizmo", "High performance gizmo", 149.99m, "GBP", 40, "Gizmos"),
        };

        foreach (var p in seed)
        {
            p.ClearDomainEvents();
            _store[p.Id] = p;
        }
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Product>>(_store.Values.Where(p => p.IsActive).ToList());

    public Task<IReadOnlyList<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Product>>(
            _store.Values.Where(p => p.IsActive && p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _store[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _store[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var product))
            product.Deactivate();
        return Task.CompletedTask;
    }
}
