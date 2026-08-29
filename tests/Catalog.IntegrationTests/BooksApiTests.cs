using System.Net;
using System.Net.Http.Json;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.IntegrationTests;

public class BooksApiTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    [Fact]
    public async Task CreateBook_WithValidRequest_Returns201Created()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/books", new
        {
            title = "Test Book",
            isbn = "9780134494167",
            description = "A test book",
            author = "Jane Doe",
            price = 19.99m,
            currency = "USD"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<BookDto>();
        dto.Should().NotBeNull();
        dto!.Title.Should().Be("Test Book");
        dto.Isbn.Should().Be("9780134494167");
        dto.Id.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task GetBook_WithValidId_ReturnsBook()
    {
        var created = await CreateBookAsync();
        var id = created.Id;

        var response = await _client.GetAsync($"/api/v1/books/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BookDto>();
        dto!.Title.Should().Be("Integration Book");
    }

    [Fact]
    public async Task GetBook_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/books/00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        error!.Status.Should().Be(404);
        error.Title.Should().Be("Resource not found");
    }

    [Fact]
    public async Task CreateBook_WithEmptyTitle_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/books", new
        {
            title = "",
            isbn = "9780134494167",
            description = "x",
            author = "Author",
            price = -5m,
            currency = "USD"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("validation");
    }

    [Fact]
    public async Task GetAll_PersistedBooksAreReturnedPaginated()
    {
        await CreateBookAsync("Alpha Book", "9780134494168");
        await CreateBookAsync("Beta Book", "9780134494169");

        var response = await _client.GetAsync("/api/v1/books?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<BookDto>>();
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().Contain(b => b.Title == "Alpha Book");
    }

    private async Task<BookDto> CreateBookAsync(string title = "Integration Book", string isbn = "9780134494170")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/books", new
        {
            title,
            isbn,
            description = "auto",
            author = "Author",
            price = 10m,
            currency = "USD"
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookDto>())!;
    }
}