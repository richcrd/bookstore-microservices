namespace Catalog.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier of a book
/// </summary>
public readonly record struct BookId(Guid Value)
{
    /// <summary>
    /// Creates a new unique book identifier
    /// </summary>
    public static BookId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
