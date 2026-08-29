using MassTransit;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.Events;
using SharedKernel.Messages;

namespace Orders.Infrastructure.Data.Repositories;

public class UnitOfWork(OrdersDbContext context, IPublishEndpoint publishEndpoint) : IUnitOfWork
{
    public void Dispose() => context.Dispose();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await PublishDomainEventsAsync(cancellationToken);
        return await context.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        var orders = context.ChangeTracker.Entries<Order>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var order in orders)
        {
            var items = order.Items
                .Select(i => new OrderItemMessage(i.BookId, i.Quantity))
                .ToList();

            foreach (var domainEvent in order.DomainEvents)
            {
                switch (domainEvent)
                {
                    case OrderCreated created:
                        await publishEndpoint.Publish(new OrderCreatedMessage(
                            created.OrderId,
                            created.CustomerId,
                            order.Total.Amount,
                            order.Total.Currency,
                            created.OccurredOn,
                            items), cancellationToken);
                        break;
                    case OrderStatusChanged changed:
                        await publishEndpoint.Publish(new OrderStatusChangedMessage(
                            changed.OrderId,
                            order.CustomerId,
                            changed.OldStatus.ToString(),
                            changed.NewStatus.ToString(),
                            changed.OccurredOn,
                            items), cancellationToken);
                        break;
                }
            }

            order.ClearDomainEvents();
        }
    }
    
}