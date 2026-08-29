namespace Inventory.Domain.Events;

public record StockCreated(Guid StockId, Guid BookId, int Quantity) : DomainEvent;