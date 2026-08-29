namespace Inventory.Domain.Events;

public record StockRestocked(Guid StockId, Guid BookId, int Quantity) : DomainEvent;