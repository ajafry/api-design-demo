using SharedKernel;
using OrderService.Domain.Enums;

namespace OrderService.Domain.Events;

public record OrderCreatedEvent(Guid OrderId, string CustomerId, decimal TotalAmount) : IDomainEvent;

public record OrderStatusChangedEvent(Guid OrderId, OrderStatus NewStatus) : IDomainEvent;
