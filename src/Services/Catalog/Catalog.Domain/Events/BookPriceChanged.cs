using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Events;

public record BookPriceChanged(
    Guid BookId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency
    ) : DomainEvent;