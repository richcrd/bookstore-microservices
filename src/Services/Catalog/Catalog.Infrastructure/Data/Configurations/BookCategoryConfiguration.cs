using Catalog.Domain.Entities;
using Catalog.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Data.Configurations;

public class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
{
    public void Configure(EntityTypeBuilder<BookCategory> builder)
    {
        builder.ToTable("book_categories");

        builder.HasKey(bc => new { bc.BookId, bc.CategoryId });

        builder.Property(bc => bc.BookId)
            .HasConversion(StronglyTypedIdConverters.BookId);

        builder.Property(bc => bc.CategoryId)
            .HasConversion(StronglyTypedIdConverters.CategoryId);
    }
}