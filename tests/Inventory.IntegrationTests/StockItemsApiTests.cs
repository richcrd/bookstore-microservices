using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Application.DTOs;
using Inventory.Application.Queries;

namespace Inventory.IntegrationTests;

public class StockItemsApiTests(InventoryApiFactory factory) : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ValidRequest_ShouldReturnCreated()
    {
        var bookId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync("/api/v1/stock-items",
            new CreateStockItemRequest(bookId, 10));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var stock = await response.Content.ReadFromJsonAsync<StockItemDto>();
        stock!.BookId.Should().Be(bookId);
        stock.QuantityOnHand.Should().Be(10);
        stock.Available.Should().Be(10);
    }

    [Fact]
    public async Task Reserve_ShouldReduceAvailable()
    {
        var bookId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/v1/stock-items", new CreateStockItemRequest(bookId, 10));

        var response = await _client.PostAsJsonAsync($"/api/v1/stock-items/{bookId}/reserve",
            new StockOperationRequest(3));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stock = await response.Content.ReadFromJsonAsync<StockItemDto>();
        stock!.ReservedQuantity.Should().Be(3);
        stock.Available.Should().Be(7);
    }

    [Fact]
    public async Task Reserve_MoreThanAvailable_ShouldReturnBadRequest()
    {
        var bookId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/v1/stock-items", new CreateStockItemRequest(bookId, 10));

        var response = await _client.PostAsJsonAsync($"/api/v1/stock-items/{bookId}/reserve",
            new StockOperationRequest(100));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DuplicateBook_ShouldReturnBadRequest()
    {
        var bookId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/v1/stock-items", new CreateStockItemRequest(bookId, 5));

        var response = await _client.PostAsJsonAsync("/api/v1/stock-items",
            new CreateStockItemRequest(bookId, 7));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByBook_ShouldReturnStock()
    {
        var bookId = Guid.NewGuid();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/stock-items",
            new CreateStockItemRequest(bookId, 8));
        var created = await createResponse.Content.ReadFromJsonAsync<StockItemDto>();

        var response = await _client.GetAsync($"/api/v1/stock-items/by-book/{bookId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stock = await response.Content.ReadFromJsonAsync<StockItemDto>();
        stock!.Id.Should().Be(created!.Id);
    }
}