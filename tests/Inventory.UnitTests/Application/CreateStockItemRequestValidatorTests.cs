using FluentAssertions;
using FluentValidation.TestHelper;
using Inventory.Application.Commands.Validation;
using Inventory.Application.DTOs;

namespace Inventory.UnitTests.Application;

public class CreateStockItemRequestValidatorTests
{
    private readonly CreateStockItemRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        var request = new CreateStockItemRequest(Guid.NewGuid(), 5);

        _validator.TestValidate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyBookId_ShouldFail()
    {
        var request = new CreateStockItemRequest(Guid.Empty, 5);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.BookId);
    }

    [Fact]
    public void NegativeInitialQuantity_ShouldFail()
    {
        var request = new CreateStockItemRequest(Guid.NewGuid(), -1);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.InitialQuantity);
    }
}