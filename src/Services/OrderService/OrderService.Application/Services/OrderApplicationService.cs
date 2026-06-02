using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;
using SharedKernel;

namespace OrderService.Application.Services;

public class OrderApplicationService(IOrderRepository repository) : IOrderService
{
    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken ct = default)
    {
        var orders = await repository.GetAllAsync(ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await repository.GetByIdAsync(id, ct);
        return order is null ? null : ToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetByCustomerAsync(string customerId, CancellationToken ct = default)
    {
        var orders = await repository.GetByCustomerAsync(customerId, ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<Result<Guid>> CreateAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        var items = command.Items.Select(i =>
            OrderItem.Create(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Currency));

        var order = Order.Create(command.CustomerId, items);
        await repository.AddAsync(order, ct);
        return Result<Guid>.Success(order.Id);
    }

    public async Task<Result> UpdateStatusAsync(UpdateOrderStatusCommand command, CancellationToken ct = default)
    {
        var order = await repository.GetByIdAsync(command.OrderId, ct);
        if (order is null) return Result.Failure($"Order {command.OrderId} not found.");

        try
        {
            order.UpdateStatus(command.NewStatus);
            await repository.UpdateAsync(order, ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.CustomerId, o.Status, o.Status.ToString(),
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Currency, i.LineTotal)).ToList(),
        o.TotalAmount, o.Currency, o.CreatedAt, o.UpdatedAt);
}
