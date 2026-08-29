using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Catalog.Infrastructure.Data.Converters;

/// <summary>
/// ValueConverter(TModel, TProvider), the first type generic is domain type (BookId),
/// the second one is stored in db (Guid)
/// </summary>
internal static class StronglyTypedIdConverters
{
    public static readonly ValueConverter<BookId, Guid> BookId = new(id => id.Value, value => new BookId(value));
    
    public static readonly ValueConverter<CategoryId, Guid> CategoryId = new(id => id.Value, value => new CategoryId(value));
}