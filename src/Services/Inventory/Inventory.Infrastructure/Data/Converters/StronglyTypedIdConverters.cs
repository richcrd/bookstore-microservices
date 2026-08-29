using Inventory.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Inventory.Infrastructure.Data.Converters;

public static class StronglyTypedIdConverters
{
    public static readonly ValueConverter<StockId, Guid> StockId = new(id => id.Value, value => new StockId(value));
}