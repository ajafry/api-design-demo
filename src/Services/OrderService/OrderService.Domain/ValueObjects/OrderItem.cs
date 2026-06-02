namespace OrderService.Domain.ValueObjects;

/// <summary>Value object representing a single line item within an order.</summary>
public sealed record OrderItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public string Currency { get; }

    public decimal LineTotal => UnitPrice * Quantity;

    private OrderItem(Guid productId, string productName, int quantity, decimal unitPrice, string currency)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Currency = currency;
    }

    public static OrderItem Create(Guid productId, string productName, int quantity, decimal unitPrice, string currency = "GBP")
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        return new OrderItem(productId, productName, quantity, unitPrice, currency);
    }
}
