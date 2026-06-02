using OrderService.Application.DTOs;

namespace OrderService.Application.Queries;

public record GetAllOrdersQuery();

public record GetOrderByIdQuery(Guid Id);

public record GetOrdersByCustomerQuery(string CustomerId);
