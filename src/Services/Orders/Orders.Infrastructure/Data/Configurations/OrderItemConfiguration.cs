using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;
using Orders.Infrastructure.Data.Converters;

namespace Orders.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => new { i.OrderId, i.BookId });

        builder.Property(i => i.OrderId)
            .HasConversion(StronglyTypedIdConverters.OrderId);

        builder.Property(i => i.BookId).IsRequired();
        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        builder.OwnsOne(i => i.UnitPrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("unit_price_currency").HasMaxLength(3);
        });

        builder.Ignore(i => i.LineTotal);
    }
}