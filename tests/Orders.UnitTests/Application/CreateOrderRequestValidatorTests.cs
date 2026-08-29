using FluentAssertions;
using FluentValidation.TestHelper;
using Orders.Application.Commands.Validation;
using Orders.Application.DTOs;

namespace Orders.UnitTests.Application;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);

        _validator.TestValidate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyCustomerId_ShouldFail()
    {
        var request = new CreateOrderRequest(Guid.Empty, [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void EmptyItems_ShouldFail()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), []);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void ZeroQuantity_ShouldFail()
    {
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(Guid.NewGuid(), 0)]);

        _validator.TestValidate(request).ShouldHaveValidationErrorFor("Items[0].Quantity");
    }
}