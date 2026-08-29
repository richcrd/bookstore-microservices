using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Domain.Events;

public record OrderStatusChanged(Guid OrderId, OrderStatus OldStatus, OrderStatus NewStatus) : DomainEvent;