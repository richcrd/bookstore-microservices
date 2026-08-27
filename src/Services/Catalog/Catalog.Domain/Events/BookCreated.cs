using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Events;

public record BookCreated(
    Guid BookId,
    string Title,
    string Isbn
    ) : DomainEvent;