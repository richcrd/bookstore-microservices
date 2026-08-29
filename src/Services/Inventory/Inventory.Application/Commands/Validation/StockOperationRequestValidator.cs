using FluentValidation;
using Inventory.Application.DTOs;

namespace Inventory.Application.Commands.Validation;

public class StockOperationRequestValidator : AbstractValidator<StockOperationRequest>
{
    public StockOperationRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}