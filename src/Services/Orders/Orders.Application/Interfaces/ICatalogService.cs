namespace Orders.Application.Interfaces;

public record BookSnapshot(Guid Id, string Title, decimal Price, string Currency);

public interface ICatalogService
{
    Task<BookSnapshot> GetBookAsync(Guid bookId, CancellationToken cancellationToken = default);
}