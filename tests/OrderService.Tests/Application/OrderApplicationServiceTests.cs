using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderService.Application.Commands;
using OrderService.Application.Contracts;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Application;

public class OrderApplicationServiceTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalogClient _catalog = Substitute.For<IProductCatalogClient>();
    private readonly OrderApplicationService _sut;

    private static readonly Guid ProductId1 = Guid.NewGuid();
    private static readonly Guid ProductId2 = Guid.NewGuid();

    public OrderApplicationServiceTests()
    {
        _sut = new OrderApplicationService(_repository, _catalog);
    }

    // ---- Helpers ----

    private static ProductLookup ActiveProduct(Guid id, string name = "Widget", decimal price = 10m) =>
        new(id, name, price, "GBP", IsActive: true);

    private static OrderItemRequest ItemRequest(Guid productId, int qty = 1) =>
        new(productId, "ignored-name", qty, 999m, "USD"); // name/price/currency should be overridden by catalogue

    // ---- GetAllAsync ----

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var orders = new[] { CreatePersistedOrder("c1") };
        _repository.GetAllAsync(default).ReturnsForAnyArgs(orders);

        var result = await _sut.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("c1", result[0].CustomerId);
    }

    // ---- GetByIdAsync ----

    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsDto()
    {
        var order = CreatePersistedOrder("c1");
        _repository.GetByIdAsync(order.Id, default).ReturnsForAnyArgs(order);

        var result = await _sut.GetByIdAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), default).ReturnsForAnyArgs((Order?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ---- CreateAsync — validation guards ----

    [Fact]
    public async Task CreateAsync_EmptyItems_ReturnsFailure()
    {
        var command = new CreateOrderCommand("c1", []);

        var result = await _sut.CreateAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("at least one item", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ProductNotFound_ReturnsFailure()
    {
        // catalogue returns null for any product ID
        _catalog.GetByIdAsync(Arg.Any<Guid>(), default).ReturnsForAnyArgs((ProductLookup?)null);
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1)]);

        // we expect a failure because the product ID in the command doesn't exist in the catalogue, as it
        // always returns null
        var result = await _sut.CreateAsync(command);

        Assert.False(result.IsSuccess);         // Failure is what we expect here
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_InactiveProduct_ReturnsFailure()
    {
        var inactive = new ProductLookup(ProductId1, "Widget", 10m, "GBP", IsActive: false);
        _catalog.GetByIdAsync(ProductId1, default).ReturnsForAnyArgs(inactive);
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1)]);

        var result = await _sut.CreateAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("no longer available", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_CatalogUnreachable_ReturnsFailure()
    {
        _catalog.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1)]);

        var result = await _sut.CreateAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("Product Catalogue", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ---- CreateAsync — happy path ----

    [Fact]
    public async Task CreateAsync_ValidProducts_ReturnsSuccessWithOrderId()
    {
        _catalog.GetByIdAsync(ProductId1, default).ReturnsForAnyArgs(ActiveProduct(ProductId1));
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1, qty: 3)]);

        var result = await _sut.CreateAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _repository.ReceivedWithAnyArgs(1).AddAsync(Arg.Any<Order>(), default);
    }

    [Fact]
    public async Task CreateAsync_UsesCataloguePriceNotClientPrice()
    {
        // catalogue says £15, client claims £999
        _catalog.GetByIdAsync(ProductId1, default).ReturnsForAnyArgs(ActiveProduct(ProductId1, price: 15m));
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1, qty: 2)]);
        Order? savedOrder = null;
        await _repository.AddAsync(Arg.Do<Order>(o => savedOrder = o), default);

        await _sut.CreateAsync(command);

        Assert.NotNull(savedOrder);
        Assert.Equal(30m, savedOrder!.TotalAmount); // 2 × £15, not 2 × £999
    }

    [Fact]
    public async Task CreateAsync_UsesCatalogueNameNotClientName()
    {
        _catalog.GetByIdAsync(ProductId1, default).ReturnsForAnyArgs(
            new ProductLookup(ProductId1, "Official Name", 10m, "GBP", true));
        Order? savedOrder = null;
        await _repository.AddAsync(Arg.Do<Order>(o => savedOrder = o), default);
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1)]);

        await _sut.CreateAsync(command);

        Assert.Equal("Official Name", savedOrder!.Items[0].ProductName);
    }

    [Fact]
    public async Task CreateAsync_MultipleProducts_AllValidated()
    {
        _catalog.GetByIdAsync(ProductId1, default).ReturnsForAnyArgs(ActiveProduct(ProductId1, price: 10m));
        _catalog.GetByIdAsync(ProductId2, default).ReturnsForAnyArgs(ActiveProduct(ProductId2, price: 20m));
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1), ItemRequest(ProductId2)]);

        var result = await _sut.CreateAsync(command);

        Assert.True(result.IsSuccess);
        await _catalog.ReceivedWithAnyArgs(2).GetByIdAsync(Arg.Any<Guid>(), default);
    }

    [Fact]
    public async Task CreateAsync_SecondProductInvalid_ReturnsFailureWithoutSaving()
    {
        _catalog.GetByIdAsync(ProductId1, default).ReturnsForAnyArgs(ActiveProduct(ProductId1));
        _catalog.GetByIdAsync(ProductId2, default).ReturnsForAnyArgs((ProductLookup?)null);
        var command = new CreateOrderCommand("c1", [ItemRequest(ProductId1), ItemRequest(ProductId2)]);

        var result = await _sut.CreateAsync(command);

        Assert.False(result.IsSuccess);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<Order>(), default);
    }

    // ---- UpdateStatusAsync ----

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_ReturnsSuccess()
    {
        var order = CreatePersistedOrder("c1");
        _repository.GetByIdAsync(order.Id, default).ReturnsForAnyArgs(order);
        var command = new UpdateOrderStatusCommand(order.Id, OrderStatus.Confirmed);

        var result = await _sut.UpdateStatusAsync(command);

        Assert.True(result.IsSuccess);
        await _repository.ReceivedWithAnyArgs(1).UpdateAsync(Arg.Any<Order>(), default);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownOrder_ReturnsFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), default).ReturnsForAnyArgs((Order?)null);
        var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Confirmed);

        var result = await _sut.UpdateStatusAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ReturnsFailure()
    {
        var order = CreatePersistedOrder("c1"); // status = Pending
        _repository.GetByIdAsync(order.Id, default).ReturnsForAnyArgs(order);
        // Pending → Delivered is not a valid transition
        var command = new UpdateOrderStatusCommand(order.Id, OrderStatus.Delivered);

        var result = await _sut.UpdateStatusAsync(command);

        Assert.False(result.IsSuccess);
    }

    // ---- Private helper ----

    private static Order CreatePersistedOrder(string customerId)
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Widget", 1, 10m);
        return Order.Create(customerId, [item]);
    }
}
