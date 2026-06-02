using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using SharedKernel;

namespace OrderService.Application.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken ct = default);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> GetByCustomerAsync(string customerId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(CreateOrderCommand command, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(UpdateOrderStatusCommand command, CancellationToken ct = default);
}
