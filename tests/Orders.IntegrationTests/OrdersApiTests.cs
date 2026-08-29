using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Application.DTOs;
using Orders.Application.Queries;

namespace Orders.IntegrationTests;

public class OrdersApiTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task PostOrder_ValidRequest_ShouldReturnCreated()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(FakeCatalogService.KnownBookId, 2)]);

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Status.Should().Be("Pending");
        order.Total.Should().Be(399.98m);
        order.Items.Should().ContainSingle().Which.Title.Should().Be("Clean Architecture V2");
    }

    [Fact]
    public async Task PostOrder_UnknownBook_ShouldReturnBadRequest()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostOrder_EmptyItems_ShouldReturnValidationError()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), []);

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrderById_ExistingOrder_ShouldReturnIt()
    {
        var createRequest = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(FakeCatalogService.KnownBookId, 1)]);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var response = await _client.GetAsync($"/api/v1/orders/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Id.Should().Be(created.Id);
        order.Total.Should().Be(199.99m);
    }

    [Fact]
    public async Task GetOrders_ShouldReturnPaginatedList()
    {
        var createRequest = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(FakeCatalogService.KnownBookId, 1)]);

        await _client.PostAsJsonAsync("/api/v1/orders", createRequest);

        var response = await _client.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<OrderDto>>();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostOrder_SameIdempotencyKey_ShouldReturnSameOrderWithoutDuplicates()
    {
        var customerId = Guid.NewGuid();
        var key = Guid.NewGuid().ToString("D");
        var request = new CreateOrderRequest(
            customerId,
            [new CreateOrderItemRequest(FakeCatalogService.KnownBookId, 1)]);

        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        first.Headers.Add("Idempotency-Key", key);
        var firstResponse = await _client.SendAsync(first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstOrder = await firstResponse.Content.ReadFromJsonAsync<OrderDto>();

        using var second = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        second.Headers.Add("Idempotency-Key", key);
        var secondResponse = await _client.SendAsync(second);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondOrder = await secondResponse.Content.ReadFromJsonAsync<OrderDto>();

        secondOrder!.Id.Should().Be(firstOrder!.Id);

        var list = await _client.GetFromJsonAsync<PaginatedResult<OrderDto>>(
            $"/api/v1/orders?customerId={customerId}");
        list!.Items.Should().ContainSingle(i => i.Id == firstOrder.Id);
    }

    [Fact]
    public async Task PostOrder_InvalidIdempotencyKey_ShouldReturnBadRequest()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(FakeCatalogService.KnownBookId, 1)]);

        using var invalid = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        invalid.Headers.Add("Idempotency-Key", "no-soy-un-guid");

        var response = await _client.SendAsync(invalid);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}