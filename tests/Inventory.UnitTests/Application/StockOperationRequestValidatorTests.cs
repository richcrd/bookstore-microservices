using FluentAssertions;
using FluentValidation.TestHelper;
using Inventory.Application.Commands.Validation;
using Inventory.Application.DTOs;

namespace Inventory.UnitTests.Application;

public class StockOperationRequestValidatorTests
{
    private readonly StockOperationRequestValidator _validator = new();

    [Fact]
    public void PositiveQuantity_ShouldPass()
    {
        _validator.TestValidate(new StockOperationRequest(3)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveQuantity_ShouldFail(int quantity)
    {
        _validator.TestValidate(new StockOperationRequest(quantity))
            .ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}