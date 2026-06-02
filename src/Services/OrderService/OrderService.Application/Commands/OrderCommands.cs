using OrderService.Domain.Enums;

namespace OrderService.Application.Commands;

public record OrderItemRequest(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, string Currency = "GBP");

public record CreateOrderCommand(string CustomerId, IReadOnlyList<OrderItemRequest> Items);

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus);
