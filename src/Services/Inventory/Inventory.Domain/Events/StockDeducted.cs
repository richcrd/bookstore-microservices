namespace Inventory.Domain.Events;

public record StockDeducted(Guid StockId, Guid BookId, int Quantity) : DomainEvent;