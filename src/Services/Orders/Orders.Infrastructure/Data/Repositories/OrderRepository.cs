using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;

namespace Orders.Infrastructure.Data.Repositories;

public class OrderRepository(OrdersDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
        => await context.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => await context.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetOrdersAsync(Guid? customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Orders.AsNoTracking().AsQueryable();

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => await context.Orders.AddAsync(order, cancellationToken);
}