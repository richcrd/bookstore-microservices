namespace Inventory.Domain.Events;

public record StockReleased(Guid StockId, Guid BookId, int Quantity) : DomainEvent;