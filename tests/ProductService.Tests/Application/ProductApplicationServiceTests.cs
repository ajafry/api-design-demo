using NSubstitute;
using ProductService.Application.Commands;
using ProductService.Application.Services;
using ProductService.Domain.Entities;
using ProductService.Domain.Repositories;

namespace ProductService.Tests.Application;

public class ProductApplicationServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ProductApplicationService _sut;

    public ProductApplicationServiceTests()
    {
        _sut = new ProductApplicationService(_repository);
    }

    // ---- GetAllAsync ----

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var products = new[]
        {
            Product.Create("A", "desc", 1m, "GBP", 1, "Cat"),
            Product.Create("B", "desc", 2m, "GBP", 2, "Cat"),
        };
        _repository.GetAllAsync(default).ReturnsForAnyArgs(products);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "A");
        Assert.Contains(result, d => d.Name == "B");
    }

    // ---- GetByIdAsync ----

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var product = Product.Create("Widget", "desc", 9.99m, "GBP", 5, "Widgets");
        _repository.GetByIdAsync(product.Id, default).ReturnsForAnyArgs(product);

        var result = await _sut.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result!.Id);
        Assert.Equal("Widget", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), default).ReturnsForAnyArgs((Product?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ---- GetByCategoryAsync ----

    [Fact]
    public async Task GetByCategoryAsync_ReturnsDtosInCategory()
    {
        var products = new[] { Product.Create("Widget", "desc", 1m, "GBP", 1, "Widgets") };
        _repository.GetByCategoryAsync("Widgets", default).ReturnsForAnyArgs(products);

        var result = await _sut.GetByCategoryAsync("Widgets");

        Assert.Single(result);
        Assert.Equal("Widgets", result[0].Category);
    }

    // ---- CreateAsync ----

    [Fact]
    public async Task CreateAsync_ValidCommand_ReturnsSuccessWithId()
    {
        var command = new CreateProductCommand("Widget", "desc", 9.99m, "GBP", 10, "Widgets");

        var result = await _sut.CreateAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _repository.ReceivedWithAnyArgs(1).AddAsync(Arg.Any<Product>(), default);
    }

    [Fact]
    public async Task CreateAsync_MapsAllFieldsToEntity()
    {
        var command = new CreateProductCommand("Widget", "A widget", 29.99m, "USD", 50, "Widgets");
        Product? savedProduct = null;
        await _repository.AddAsync(Arg.Do<Product>(p => savedProduct = p), default);

        await _sut.CreateAsync(command);

        Assert.NotNull(savedProduct);
        Assert.Equal("Widget", savedProduct!.Name);
        Assert.Equal(29.99m, savedProduct.Price.Amount);
        Assert.Equal("USD", savedProduct.Price.Currency);
        Assert.Equal(50, savedProduct.StockQuantity);
    }

    // ---- UpdateAsync ----

    [Fact]
    public async Task UpdateAsync_ExistingProduct_ReturnsSuccess()
    {
        var product = Product.Create("Old", "desc", 1m, "GBP", 1, "Cat");
        _repository.GetByIdAsync(product.Id, default).ReturnsForAnyArgs(product);
        var command = new UpdateProductCommand(product.Id, "New", "new desc", 2m, "GBP", 5, "Cat");

        var result = await _sut.UpdateAsync(command);

        Assert.True(result.IsSuccess);
        await _repository.ReceivedWithAnyArgs(1).UpdateAsync(Arg.Any<Product>(), default);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), default).ReturnsForAnyArgs((Product?)null);
        var command = new UpdateProductCommand(Guid.NewGuid(), "N", "d", 1m, "GBP", 1, "C");

        var result = await _sut.UpdateAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ---- DeleteAsync ----

    [Fact]
    public async Task DeleteAsync_ExistingProduct_ReturnsSuccess()
    {
        var product = Product.Create("Widget", "desc", 1m, "GBP", 1, "Widgets");
        _repository.GetByIdAsync(product.Id, default).ReturnsForAnyArgs(product);

        var result = await _sut.DeleteAsync(product.Id);

        Assert.True(result.IsSuccess);
        await _repository.ReceivedWithAnyArgs(1).DeleteAsync(Arg.Any<Guid>(), default);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), default).ReturnsForAnyArgs((Product?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
