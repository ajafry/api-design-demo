using SharedKernel;
using OrderService.Domain.Enums;
using OrderService.Domain.Events;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>Order aggregate root. Owns its OrderItems and enforces order state transitions.</summary>
public sealed class Order : Entity<Guid>
{
    private readonly List<OrderItem> _items = [];

    public string CustomerId { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(i => i.LineTotal);
    public string Currency => _items.FirstOrDefault()?.Currency ?? "GBP";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Order() { }

    public static Order Create(string customerId, IEnumerable<OrderItem> items)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        order._items.AddRange(items);
        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, order.CustomerId, order.TotalAmount));
        return order;
    }

    /// <summary>Transitions the order to a new status, enforcing valid state machine rules.</summary>
    public void UpdateStatus(OrderStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderStatusChangedEvent(Id, Status));
    }

    private static bool IsValidTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => true,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Shipped) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        (OrderStatus.Shipped, OrderStatus.Delivered) => true,
        _ => false
    };
}
