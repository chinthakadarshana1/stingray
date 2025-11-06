using FluentValidation;
using Stingray.Application.Commands;
using Stingray.Application.Commands.Orders;

namespace Stingray.Application.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required")
            .MinimumLength(2).WithMessage("ProductName must be at least 2 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("TotalPrice must be greater than 0");
    }
}