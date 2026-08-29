using FluentAssertions;
using NSubstitute;
using Orders.Application;
using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;

namespace Orders.UnitTests.Application;

public class CreateOrderCommandTests
{
    [Fact]
    public async Task Execute_ShouldCreateOrder_WithSnapshotFromCatalog()
    {
        var bookId = Guid.NewGuid();
        var catalog = Substitute.For<ICatalogService>();
        catalog.GetBookAsync(bookId, Arg.Any<CancellationToken>())
            .Returns(new BookSnapshot(bookId, "Clean Architecture V2", 199.99m, "USD"));

        var repository = Substitute.For<IOrderRepository>();
        Order? added = null;
        repository.AddAsync(Arg.Do<Order>(o => added = o), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var unitOfWork = Substitute.For<IUnitOfWork>();

        var command = new CreateOrderCommand(repository, unitOfWork, catalog);

        var dto = await command.ExecuteAsync(new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(bookId, 2)]));

        dto.Status.Should().Be("Pending");
        dto.Total.Should().Be(399.98m);
        dto.Items.Should().HaveCount(1);
        dto.Items[0].Title.Should().Be("Clean Architecture V2");

        added.Should().NotBeNull();
        added!.Items.Should().HaveCount(1);
        added.Items[0].UnitPrice.Amount.Should().Be(199.99m);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}