using Catalog.Domain.ValueObjects;
using FluentAssertions;

namespace Catalog.UnitTests.Domain;

public class IsbnTests
{
    [Fact]
    public void Constructor_WithValid10DigitIsbn_Accepts()
    {
        var isbn = new Isbn("0306406152");

        isbn.Value.Should().Be("0306406152");
    }

    [Fact]
    public void Constructor_WithValid13DigitIsbn_Accepts()
    {
        var isbn = new Isbn("9780134494166");

        isbn.Value.Should().Be("9780134494166");
    }

    [Fact]
    public void Constructor_WithHyphens_StripsThem()
    {
        var isbn = new Isbn("978-0-13-449416-6");

        isbn.Value.Should().Be("9780134494166");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_WithNullOrEmpty_ThrowsArgumentException(string? isbn)
    {
        var act = () => new Isbn(isbn!);

        act.Should().Throw<ArgumentException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901234567890")]
    public void Constructor_WithInvalidLength_ThrowsArgumentException(string isbn)
    {
        var act = () => new Isbn(isbn);

        act.Should().Throw<ArgumentException>().WithMessage("*10 or 13*");
    }
}