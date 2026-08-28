using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Queries;

public class GetBookByIdQuery(IBookRepository bookRepository)
{
    public async Task<BookDto> ExecuteAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(new BookId(bookId), cancellationToken)
                   ?? throw new CatalogDomainException($"Book with ID {bookId} not found.");

        return new BookDto(
            book.Id.Value,
            book.Title,
            book.Isbn.Value,
            book.Description,
            book.Author,
            book.Price.Amount,
            book.Price.Currency,
            book.CreatedAt,
            book.UpdatedAt);
    }
    
}