using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using OrderService.Application.Commands;
using OrderService.Application.Contracts;
using OrderService.Application.DTOs;
using OrderService.Domain.Enums;

namespace OrderService.IntegrationTests;

public class OrderEndpointsTests : IClassFixture<OrderServiceWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly IProductCatalogClient _catalog;

    // Stable product ID reused across tests that need catalog lookups
    private static readonly Guid ProductId = Guid.NewGuid();

    public OrderEndpointsTests(OrderServiceWebAppFactory factory)
    {
        _client = factory.CreateClient();
        _catalog = factory.CatalogClient;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ProductLookup ActiveProduct(Guid id) =>
        new(id, "Widget Pro", 19.99m, "GBP", IsActive: true);

    private static CreateOrderCommand SingleItemOrder(Guid productId) =>
        new("customer-integration", [new OrderItemRequest(productId, "ignored", 2, 99m, "USD")]);

    /// Creates a Pending order and returns its ID.
    private async Task<Guid> CreateOrderAsync(Guid productId)
    {
        _catalog.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ActiveProduct(productId));

        var response = await _client.PostAsJsonAsync("api/orders", SingleItemOrder(productId));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    // ── GET /api/orders ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithAtLeastSeededOrder()
    {
        var response = await _client.GetAsync("api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.NotNull(orders);
        Assert.NotEmpty(orders);
    }

    // ── GET /api/orders/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsOk()
    {
        var all = await _client.GetFromJsonAsync<List<OrderDto>>("api/orders");
        var seededId = all![0].Id;

        var response = await _client.GetAsync($"api/orders/{seededId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(seededId, order!.Id);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/orders/customer/{customerId} ─────────────────────────────────

    [Fact]
    public async Task GetByCustomer_ReturnsOrdersForThatCustomerOnly()
    {
        var response = await _client.GetAsync("api/orders/customer/customer-001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.NotNull(orders);
        Assert.All(orders, o => Assert.Equal("customer-001", o.CustomerId));
    }

    // ── POST /api/orders ──────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidProducts_ReturnsCreated_WithOrderId()
    {
        _catalog.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ActiveProduct(ProductId));

        var response = await _client.PostAsJsonAsync("api/orders", SingleItemOrder(ProductId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var orderId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, orderId);
    }

    [Fact]
    public async Task Create_UsesCataloguePriceNotClientPrice()
    {
        // Catalog says £19.99; client sends £99 — order total must use catalog price
        // we have initialized a product with price = 19.99, qty = 2 → total = 39.98
        _catalog.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ActiveProduct(ProductId)); 

        var response = await _client.PostAsJsonAsync("api/orders", SingleItemOrder(ProductId));
        var orderId = await response.Content.ReadFromJsonAsync<Guid>();

        // The actual order creation takes the real product price, not what the client
        // passed in the request. So we expect 2 × £19.99 = £39.98, not 2 × £99.
        var order = await _client.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}");
        Assert.Equal(39.98m, order!.TotalAmount);
    }

    [Fact]
    public async Task Create_EmptyItems_ReturnsBadRequest()
    {
        var command = new CreateOrderCommand("c1", []);

        var response = await _client.PostAsJsonAsync("api/orders", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ProductNotFoundInCatalogue_ReturnsBadRequest()
    {
        _catalog.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProductLookup?)null);

        var response = await _client.PostAsJsonAsync("api/orders", SingleItemOrder(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_InactiveProduct_ReturnsBadRequest()
    {
        _catalog.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProductLookup(ProductId, "Discontinued", 5m, "GBP", IsActive: false));

        var response = await _client.PostAsJsonAsync("api/orders", SingleItemOrder(ProductId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── PATCH /api/orders/{id}/status ─────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidTransition_ReturnsNoContent()
    {
        var orderId = await CreateOrderAsync(ProductId);

        var response = await _client.PatchAsJsonAsync(
            $"api/orders/{orderId}/status",
            new { Status = (int)OrderStatus.Confirmed });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_OrderReflectsNewStatus_AfterUpdate()
    {
        var orderId = await CreateOrderAsync(ProductId);
        await _client.PatchAsJsonAsync(
            $"api/orders/{orderId}/status",
            new { Status = (int)OrderStatus.Confirmed });

        var order = await _client.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}");

        Assert.Equal(OrderStatus.Confirmed, order!.Status);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsBadRequest()
    {
        var orderId = await CreateOrderAsync(ProductId);

        // Pending → Delivered skips required intermediate states
        var response = await _client.PatchAsJsonAsync(
            $"api/orders/{orderId}/status",
            new { Status = (int)OrderStatus.Delivered });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"api/orders/{Guid.NewGuid()}/status",
            new { Status = (int)OrderStatus.Confirmed });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /health ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
