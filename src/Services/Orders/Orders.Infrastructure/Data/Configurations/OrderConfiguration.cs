using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;
using Orders.Infrastructure.Data.Converters;

namespace Orders.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(StronglyTypedIdConverters.OrderId)
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.DomainEvents);
    }
}