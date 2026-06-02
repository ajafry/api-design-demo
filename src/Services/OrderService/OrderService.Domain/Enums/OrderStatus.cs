using SharedKernel;

namespace OrderService.Domain.Enums;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
