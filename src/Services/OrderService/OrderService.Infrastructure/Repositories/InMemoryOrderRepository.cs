using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace OrderService.Infrastructure.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _store = [];

    public InMemoryOrderRepository()
    {
        // Seed with demo data
        var seed = Order.Create("customer-001", new[]
        {
            OrderItem.Create(Guid.NewGuid(), "Widget Pro", 2, 29.99m, "GBP"),
            OrderItem.Create(Guid.NewGuid(), "Gadget Lite", 1, 9.99m, "GBP")
        });
        seed.ClearDomainEvents();
        _store[seed.Id] = seed;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(_store.Values.ToList());

    public Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Order>>(
            _store.Values.Where(o => o.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _store[order.Id] = order;
        return Task.CompletedTask;
    }
}
