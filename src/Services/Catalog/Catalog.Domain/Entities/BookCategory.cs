using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

/// <summary>
/// Represents the association between a book and a category
/// </summary>
/// <remarks>
/// This type models the many-to-many relationship between <see cref="Book"/>
/// and <see cref="Category"/>. The relationship is identified by the
/// combination of <see cref="BookId"/> and <see cref="CategoryId"/>
/// </remarks>
public record BookCategory(BookId BookId, CategoryId CategoryId);