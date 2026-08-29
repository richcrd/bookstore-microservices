using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Orders.Domain.ValueObjects;

namespace Orders.Infrastructure.Data.Converters;

public static class StronglyTypedIdConverters
{
    public static readonly ValueConverter<OrderId, Guid> OrderId = new(id => id.Value, value => new OrderId(value));
}