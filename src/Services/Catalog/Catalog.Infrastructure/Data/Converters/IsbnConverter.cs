using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Catalog.Infrastructure.Data.Converters;

internal static class IsbnConverter
{
    public static readonly ValueConverter<Isbn, string> Isbn = new(isbn => isbn.Value, value => new Isbn(value));
}