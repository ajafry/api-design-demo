using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

public class OrderTests
{
    private static OrderItem MakeItem(decimal price = 10m, int qty = 2) =>
        OrderItem.Create(Guid.NewGuid(), "Widget", qty, price);

    // ---- Create ----

    [Fact]
    public void Create_ValidArgs_ReturnsPendingOrder()
    {
        var order = Order.Create("customer-1", [MakeItem()]);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("customer-1", order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.Items);
    }

    [Fact]
    public void Create_RaisesOrderCreatedEvent()
    {
        var item = MakeItem(price: 5m, qty: 4);
        var order = Order.Create("customer-1", [item]);

        var evt = Assert.Single(order.DomainEvents);
        var created = Assert.IsType<OrderCreatedEvent>(evt);
        Assert.Equal(order.Id, created.OrderId);
        Assert.Equal("customer-1", created.CustomerId);
        Assert.Equal(20m, created.TotalAmount);
    }

    [Fact]
    public void TotalAmount_SumsAllLineItems()
    {
        var items = new[]
        {
            OrderItem.Create(Guid.NewGuid(), "A", 2, 10m),
            OrderItem.Create(Guid.NewGuid(), "B", 3, 5m),
        };
        var order = Order.Create("c1", items);

        Assert.Equal(35m, order.TotalAmount); // 20 + 15
    }

    [Fact]
    public void Currency_DefaultsToFirstItemCurrency()
    {
        var order = Order.Create("c1", [OrderItem.Create(Guid.NewGuid(), "A", 1, 10m, "USD")]);

        Assert.Equal("USD", order.Currency);
    }

    // ---- UpdateStatus — valid transitions ----

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public void UpdateStatus_ValidTransition_Succeeds(OrderStatus from, OrderStatus to)
    {
        var order = Order.Create("c1", [MakeItem()]);
        order.ClearDomainEvents();

        // Drive the order to the `from` state
        AdvanceTo(order, from);
        order.ClearDomainEvents();

        order.UpdateStatus(to);

        Assert.Equal(to, order.Status);
        var evt = Assert.Single(order.DomainEvents);
        var changed = Assert.IsType<OrderStatusChangedEvent>(evt);
        Assert.Equal(to, changed.NewStatus);
    }

    // ---- UpdateStatus — invalid transitions ----

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    public void UpdateStatus_InvalidTransition_Throws(OrderStatus from, OrderStatus to)
    {
        var order = Order.Create("c1", [MakeItem()]);
        AdvanceTo(order, from);

        Assert.Throws<InvalidOperationException>(() => order.UpdateStatus(to));
    }

    // Helper: drive an order to a specific status along the happy path
    private static void AdvanceTo(Order order, OrderStatus target)
    {
        if (target == OrderStatus.Pending) return;
        if (order.Status == OrderStatus.Pending && target != OrderStatus.Pending)
        {
            if (target == OrderStatus.Cancelled) { order.UpdateStatus(OrderStatus.Cancelled); return; }
            order.UpdateStatus(OrderStatus.Confirmed);
        }
        if (order.Status == OrderStatus.Confirmed && target != OrderStatus.Confirmed)
        {
            if (target == OrderStatus.Cancelled) { order.UpdateStatus(OrderStatus.Cancelled); return; }
            order.UpdateStatus(OrderStatus.Shipped);
        }
        if (order.Status == OrderStatus.Shipped && target == OrderStatus.Delivered)
            order.UpdateStatus(OrderStatus.Delivered);
    }
}
