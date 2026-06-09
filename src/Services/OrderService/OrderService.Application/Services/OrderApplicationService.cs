using OrderService.Application.Commands;
using OrderService.Application.Contracts;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;
using SharedKernel;

namespace OrderService.Application.Services;

public class OrderApplicationService(
    IOrderRepository repository,
    IProductCatalogClient productCatalog) : IOrderService
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
        if (command.Items is not { Count: > 0 })
            return Result<Guid>.Failure("An order must contain at least one item.");

        // Validate each product against the catalogue and use authoritative price/currency.
        var enrichedItems = new List<OrderItem>();
        foreach (var item in command.Items)
        {
            ProductLookup? product;
            try
            {
                product = await productCatalog.GetByIdAsync(item.ProductId, ct);
            }
            catch (HttpRequestException ex)
            {
                return Result<Guid>.Failure($"Could not reach Product Catalogue: {ex.Message}");
            }

            if (product is null)
                return Result<Guid>.Failure($"Product {item.ProductId} not found in the catalogue.");

            if (!product.IsActive)
                return Result<Guid>.Failure($"Product '{product.Name}' ({item.ProductId}) is no longer available.");

            enrichedItems.Add(OrderItem.Create(
                product.Id,
                product.Name,
                item.Quantity,
                product.Price,      // authoritative catalogue price — ignores client-supplied price
                product.Currency));
        }

        var order = Order.Create(command.CustomerId, enrichedItems);
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
