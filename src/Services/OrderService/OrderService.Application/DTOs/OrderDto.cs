using OrderService.Domain.Enums;

namespace OrderService.Application.DTOs;

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, string Currency, decimal LineTotal);

public record OrderDto(
    Guid Id,
    string CustomerId,
    OrderStatus Status,
    string StatusName,
    IReadOnlyList<OrderItemDto> Items,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
