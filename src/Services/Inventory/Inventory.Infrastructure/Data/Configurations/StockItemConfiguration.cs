using Inventory.Domain.Entities;
using Inventory.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(StronglyTypedIdConverters.StockId)
            .ValueGeneratedNever();

        builder.Property(s => s.BookId).IsRequired();
        builder.HasIndex(s => s.BookId).IsUnique();

        builder.Property(s => s.QuantityOnHand).IsRequired();
        builder.Property(s => s.ReservedQuantity).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt);

        builder.Ignore(s => s.Available);
        builder.Ignore(s => s.DomainEvents);
    }
}