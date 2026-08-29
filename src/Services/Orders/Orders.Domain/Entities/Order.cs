using Orders.Domain.Enums;
using Orders.Domain.Events;
using Orders.Domain.ValueObjects;

namespace Orders.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public OrderId Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Money Total => Items.Aggregate(Money.Zero(),
        (total, item) => total.Add(item.LineTotal));
    
    private Order() { }

    public static Order Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        }

        var order = new Order()
        {
            Id = OrderId.New(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        order._domainEvents.Add(new OrderCreated(order.Id.Value, order.CustomerId));

        return order;
    }

    public void AddItem(Guid bookId, string title, Money unitPrice, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }
        
        _items.Add(new OrderItem(Id, bookId, title, unitPrice, quantity));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ChangeStatus(OrderStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot change order status from {Status} to {newStatus}.");
        }

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new OrderStatusChanged(Id.Value, oldStatus, newStatus));
    }
    
    private bool CanTransitionTo(OrderStatus newStatus) => newStatus switch
    {
        _ when newStatus == Status => true,
        OrderStatus.Paid => Status == OrderStatus.Pending,
        OrderStatus.Shipped => Status == OrderStatus.Paid,
        OrderStatus.Delivered => Status == OrderStatus.Shipped,
        OrderStatus.Cancelled => Status is OrderStatus.Pending or OrderStatus.Paid,
        _ => false
    };
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}