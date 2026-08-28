using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Interfaces;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(BookId id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Book> Items, int TotalCount)> GetBooksAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Book book, CancellationToken cancellationToken = default);

    void Update(Book book);

    void Remove(Book book);
}
