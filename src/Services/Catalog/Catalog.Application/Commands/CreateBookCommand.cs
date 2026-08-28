using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Commands;

public class CreateBookCommand(IBookRepository bookRepository, IUnitOfWork unitOfWork)
{
    public async Task<BookDto> ExecuteAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        var isbn = new Isbn(request.Isbn);
        var price = new Money(request.Price, request.Currency);

        var book = Book.Create(
            request.Title,
            isbn,
            request.Description,
            request.Author,
            price);

        await bookRepository.AddAsync(book, cancellationToken);
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