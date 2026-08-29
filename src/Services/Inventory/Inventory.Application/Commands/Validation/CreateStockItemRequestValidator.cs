using FluentValidation;
using Inventory.Application.DTOs;

namespace Inventory.Application.Commands.Validation;

public class CreateStockItemRequestValidator : AbstractValidator<CreateStockItemRequest>
{
    public CreateStockItemRequestValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("BookId is required.");

        RuleFor(x => x.InitialQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Initial quantity cannot be negative.");
    }
}