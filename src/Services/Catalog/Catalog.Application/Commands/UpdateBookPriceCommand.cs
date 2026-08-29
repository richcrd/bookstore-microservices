using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Commands;

public class UpdateBookPriceCommand(IBookRepository bookRepository, IUnitOfWork unitOfWork)
{
    public async Task<BookDto> ExecuteAsync(Guid bookId, decimal newPrice, string currency, CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(new BookId(bookId), cancellationToken)
                   ?? throw new CatalogNotFoundException($"Book with ID {bookId} not found.");

        var price = new Money(newPrice, currency);
        book.UpdatePrice(price);

        await unitOfWork.SaveChangesAsync(cancellationToken);

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