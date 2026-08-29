using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using NSubstitute;

namespace Inventory.UnitTests.Application;

public class CreateStockCommandTests
{
    [Fact]
    public async Task Execute_ShouldCreateAndPersist()
    {
        var bookId = Guid.NewGuid();
        var repository = Substitute.For<IStockItemRepository>();
        repository.GetByBookIdAsync(bookId, Arg.Any<CancellationToken>()).Returns((StockItem?)null);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var command = new CreateStockCommand(repository, unitOfWork);

        var dto = await command.ExecuteAsync(new CreateStockItemRequest(bookId, 10));

        dto.QuantityOnHand.Should().Be(10);
        dto.Available.Should().Be(10);

        await repository.Received(1).AddAsync(Arg.Any<StockItem>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DuplicateBook_ShouldThrowAndNotPersist()
    {
        var bookId = Guid.NewGuid();
        var repository = Substitute.For<IStockItemRepository>();
        repository.GetByBookIdAsync(bookId, Arg.Any<CancellationToken>())
            .Returns(StockItem.Create(bookId, 5));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var command = new CreateStockCommand(repository, unitOfWork);

        Func<Task> act = () => command.ExecuteAsync(new CreateStockItemRequest(bookId, 10));

        await act.Should().ThrowAsync<InventoryDomainException>();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}