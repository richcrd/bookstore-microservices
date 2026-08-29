using Catalog.Domain.Entities;
using Catalog.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");

        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Id)
            .HasConversion(StronglyTypedIdConverters.BookId)
            .ValueGeneratedNever(); // It does not generate new ID because of our Domain BookId.New()
        
        builder.Property(b => b.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Isbn)
            .HasConversion(IsbnConverter.Isbn)
            .HasMaxLength(13)
            .IsRequired();

        builder.HasIndex(b => b.Isbn).IsUnique();

        builder.Property(b => b.Description).HasMaxLength(2000);
        builder.Property(b => b.Author).HasMaxLength(150).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt);

        // OwnsOne because Money is not a table, they are columns inside books table
        builder.OwnsOne(b => b.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2);
            price.Property(p => p.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3);
        });

        builder.HasMany(b => b.BookCategories)
            .WithOne()
            .HasForeignKey(bc => bc.BookId);

        builder.Navigation(b => b.BookCategories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(b => b.DomainEvents); // EF Core does not map this property because it's not a table
    }
}