using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Data.Repositories;

public class BookRepository(CatalogDbContext context) : IBookRepository
{
    /// <summary>
    /// Single throws exception when there are duplicates
    /// </summary>
    public async Task<Book?> GetByIdAsync(BookId id, CancellationToken cancellationToken = default)
        => await context.Books.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

    /// <summary>
    /// AsNoTracking, EF Core does not keep the change in memory, only reads.
    /// </summary>
    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Books.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Book> Items, int TotalCount)> GetBooksAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b =>
                b.Title.Contains(search) || b.Author.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(b => b.Price)
            .Skip((page - 1) * pageSize) // skips old pages records, takes the current one
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
        => await context.Books.AddAsync(book, cancellationToken);

    public void Update(Book book) => context.Books.Update(book);

    public void Remove(Book book) => context.Books.Remove(book);
}