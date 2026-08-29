using Catalog.Application.Commands;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Catalog.UnitTests.Application;

public class CreateBookCommandTests
{
    /// <summary>
    /// Substitute creates a fake repo, never touches our db
    /// </summary>
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateBookCommand _command;
    
    public CreateBookCommandTests()
    {
        _command = new CreateBookCommand(_bookRepository, _unitOfWork);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithValidRequest_SavesAndReturnsDto()
    {
        var request = new CreateBookRequest(
            "Clean Architecture",
            "9780134494166",
            "desc",
            "Robert C. Martin",
            49.99m,
            "USD");

        var result = await _command.ExecuteAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("Clean Architecture");
        result.Price.Should().Be(49.99m);
        result.Currency.Should().Be("USD");

        await _bookRepository.Received(1).AddAsync(
            Arg.Is<Book>(b => b.Title == "Clean Architecture"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_WithInvalidIsbn_ThrowsAndDoesNotSave()
    {
        var request = new CreateBookRequest(
            "Book",
            "123",
            "desc",
            "Author",
            10m,
            "USD");

        var act = () => _command.ExecuteAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();

        await _bookRepository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}