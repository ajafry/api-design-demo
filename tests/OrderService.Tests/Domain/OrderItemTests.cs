using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

public class OrderItemTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsOrderItem()
    {
        var id = Guid.NewGuid();
        var item = OrderItem.Create(id, "Widget", 3, 9.99m, "GBP");

        Assert.Equal(id, item.ProductId);
        Assert.Equal("Widget", item.ProductName);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(9.99m, item.UnitPrice);
        Assert.Equal("GBP", item.Currency);
    }

    [Fact]
    public void LineTotal_IsQuantityTimesUnitPrice()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Widget", 4, 2.50m);

        Assert.Equal(10m, item.LineTotal);
    }

    [Fact]
    public void Create_ZeroQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OrderItem.Create(Guid.NewGuid(), "Widget", 0, 9.99m));
    }

    [Fact]
    public void Create_NegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OrderItem.Create(Guid.NewGuid(), "Widget", -1, 9.99m));
    }

    [Fact]
    public void Create_NegativeUnitPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OrderItem.Create(Guid.NewGuid(), "Widget", 1, -0.01m));
    }

    [Fact]
    public void Create_ZeroPrice_IsAllowed()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Widget", 1, 0m);

        Assert.Equal(0m, item.LineTotal);
    }

    [Fact]
    public void StructuralEquality_SameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = OrderItem.Create(id, "Widget", 1, 10m, "GBP");
        var b = OrderItem.Create(id, "Widget", 1, 10m, "GBP");

        Assert.Equal(a, b);
    }

    [Fact]
    public void StructuralEquality_DifferentQuantity_AreNotEqual()
    {
        var id = Guid.NewGuid();
        var a = OrderItem.Create(id, "Widget", 1, 10m);
        var b = OrderItem.Create(id, "Widget", 2, 10m);

        Assert.NotEqual(a, b);
    }
}
