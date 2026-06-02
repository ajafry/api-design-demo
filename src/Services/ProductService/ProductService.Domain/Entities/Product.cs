using SharedKernel;
using ProductService.Domain.Events;
using ProductService.Domain.ValueObjects;

namespace ProductService.Domain.Entities;

/// <summary>
/// Product aggregate root. Encapsulates all product business rules.
/// </summary>
public sealed class Product : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public int StockQuantity { get; private set; }
    public string Category { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Product() { } // EF / deserialisation

    public static Product Create(string name, string description, decimal price, string currency, int stockQuantity, string category)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = Money.Create(price, currency),
            StockQuantity = stockQuantity,
            Category = category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Price.Amount));
        return product;
    }

    public void Update(string name, string description, decimal price, string currency, int stockQuantity, string category)
    {
        Name = name;
        Description = description;
        Price = Money.Create(price, currency);
        StockQuantity = stockQuantity;
        Category = category;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ProductUpdatedEvent(Id, Name));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
