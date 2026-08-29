namespace Inventory.Domain.Events;

public record StockReserved(Guid StockId, Guid BookId, int Quantity) : DomainEvent;