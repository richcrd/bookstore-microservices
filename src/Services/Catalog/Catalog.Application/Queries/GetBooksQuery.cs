using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;

namespace Catalog.Application.Queries;

public record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public class GetBooksQuery(IBookRepository booksRepository)
{

   public async Task<PaginatedResult<BookDto>> ExecuteAsync(string? search, int page, int pageSize,
      CancellationToken cancellationToken = default)
   {
      var (books, totalCount) = await booksRepository.GetBooksAsync(search, page, pageSize, cancellationToken);

      var dtos = books.Select(b => new BookDto(
         b.Id.Value,
         b.Title,
         b.Isbn.Value,
         b.Description,
         b.Author,
         b.Price.Amount,
         b.Price.Currency,
         b.CreatedAt,
         b.UpdatedAt)).ToList();

      return new PaginatedResult<BookDto>(dtos, totalCount, page, pageSize);
   }
}