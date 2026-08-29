namespace Orders.Domain.Events;

public record OrderCreated(Guid OrderId, Guid CustomerId) : DomainEvent;