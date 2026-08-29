using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Application.Interfaces;
using Orders.Domain.Exceptions;

namespace Orders.Infrastructure.External;

public class CatalogService(HttpClient httpClient) : ICatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    
    public async Task<BookSnapshot> GetBookAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/v1/books/{bookId}", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var book = await response.Content.ReadFromJsonAsync<CatalogBook>(JsonOptions, cancellationToken)
                       ?? throw new InvalidOperationException("Catalog returned an empty response.");

            return new BookSnapshot(book.Id, book.Title, book.Price, book.Currency);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new OrderDomainException($"Book with id {bookId} was not found.");
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Catalog API returned {(int)response.StatusCode}: {content}");
    }
    
    private sealed record CatalogBook(Guid Id, string Title, decimal Price, string Currency);
}