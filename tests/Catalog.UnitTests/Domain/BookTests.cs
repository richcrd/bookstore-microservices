using Catalog.Domain.Entities;
using Catalog.Domain.Events;
using Catalog.Domain.ValueObjects;
using FluentAssertions;

namespace Catalog.UnitTests.Domain;

public class BookTests
{
    private static readonly Isbn ValidIsbn = new("9780134494166");

    [Fact]
    public void Create_WithValidInput_CreatesBookWithGeneratedId()
    {
        var book = Book.Create(
            "Clean Architecture",
            ValidIsbn,
            "A craftsman's guide",
            "Robert C. Martin",
            new Money(49.99m, "USD"));

        book.Id.Value.Should().NotBeEmpty();
        book.Title.Should().Be("Clean Architecture");
        book.Author.Should().Be("Robert C. Martin");
        book.Price.Amount.Should().Be(49.99m);
        book.Price.Currency.Should().Be("USD");
        book.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithValidInput_RaisesBookCreatedEvent()
    {
        var book = Book.Create(
            "Clean Architecture",
            ValidIsbn,
            "A craftsman's guide",
            "Robert C. Martin",
            new Money(49.99m, "USD"));

        book.DomainEvents.Should().ContainSingle();
        var created = book.DomainEvents.OfType<BookCreated>().Single();
        created.BookId.Should().Be(book.Id.Value);
        created.Title.Should().Be("Clean Architecture");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidTitle_ThrowsArgumentException(string? title)
    {
        var act = () => Book.Create(
            title!,
            ValidIsbn,
            "description",
            "Author",
            new Money(10m, "USD"));

        act.Should().Throw<ArgumentException>().WithMessage("*title*");
    }
    
    [Fact]
    public void UpdatePrice_WithValidPrice_UpdatesAndRaisesEvent()
    {
        var book = Book.Create("Book", ValidIsbn, "desc", "Author", new Money(10m, "USD"));
        book.ClearDomainEvents();

        book.UpdatePrice(new Money(25m, "USD"));

        book.Price.Amount.Should().Be(25m);
        book.DomainEvents.Should().ContainSingle();
        var changed = book.DomainEvents.OfType<BookPriceChanged>().Single();
        changed.OldPrice.Should().Be(10m);
        changed.NewPrice.Should().Be(25m);
    }
    
    [Fact]
    public void UpdatePrice_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        var book = Book.Create("Book", ValidIsbn, "desc", "Author", new Money(10m, "USD"));

        var act = () => book.UpdatePrice(new Money(10m, "EUR"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*currency*");
    }
}