using ProductService.Domain.Entities;
using ProductService.Domain.Events;

namespace ProductService.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsActiveProduct()
    {
        var product = Product.Create("Widget", "desc", 9.99m, "GBP", 10, "Widgets");

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Widget", product.Name);
        Assert.Equal(9.99m, product.Price.Amount);
        Assert.Equal("GBP", product.Price.Currency);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal("Widgets", product.Category);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_RaisesProductCreatedEvent()
    {
        var product = Product.Create("Widget", "desc", 9.99m, "GBP", 10, "Widgets");

        var evt = Assert.Single(product.DomainEvents);
        var created = Assert.IsType<ProductCreatedEvent>(evt);
        Assert.Equal(product.Id, created.ProductId);
        Assert.Equal("Widget", created.Name);
        Assert.Equal(9.99m, created.Price);
    }

    [Fact]
    public void Update_ChangesFieldsAndRaisesEvent()
    {
        var product = Product.Create("Widget", "desc", 9.99m, "GBP", 10, "Widgets");
        product.ClearDomainEvents();

        product.Update("Widget Pro", "new desc", 19.99m, "GBP", 20, "Widgets");

        Assert.Equal("Widget Pro", product.Name);
        Assert.Equal(19.99m, product.Price.Amount);
        Assert.Equal(20, product.StockQuantity);

        var evt = Assert.Single(product.DomainEvents);
        var updated = Assert.IsType<ProductUpdatedEvent>(evt);
        Assert.Equal(product.Id, updated.ProductId);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var product = Product.Create("Widget", "desc", 9.99m, "GBP", 10, "Widgets");

        product.Deactivate();

        Assert.False(product.IsActive);
    }

    [Fact]
    public void Create_NormalisesUcaseCurrency()
    {
        var product = Product.Create("Widget", "desc", 9.99m, "gbp", 10, "Widgets");

        Assert.Equal("GBP", product.Price.Currency);
    }
}
