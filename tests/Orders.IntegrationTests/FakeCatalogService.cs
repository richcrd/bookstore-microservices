using Orders.Application.Interfaces;
using Orders.Domain.Exceptions;

namespace Orders.IntegrationTests;

public class FakeCatalogService : ICatalogService
{
    public static readonly Guid KnownBookId = Guid.Parse("c3d9ee85-1d84-473e-be03-b3e5240f9af8");
    private readonly BookSnapshot _book = new(KnownBookId, "Clean Architecture V2", 199.99m, "USD");
    
    public async Task<BookSnapshot> GetBookAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        if (bookId != _book.Id)
        {
            throw new OrderDomainException($"Book with id {bookId} was not found.");
        }

        return await Task.FromResult(_book);
    }
}