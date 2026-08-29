using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;

namespace Orders.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetOrdersAsync(
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}