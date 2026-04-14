using FluentValidation;
using OrderService2.Model;
using Shared.Enum;

namespace OrderService2.Request;

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ClientId is required")
            .NotNull().WithMessage("ClientId cannot be null")
            .Matches(@"^[a-zA-Z0-9]+$").WithMessage("ClientId must be alphanumeric");

        RuleFor(x => x.InstrumentSymbol)
            .NotEmpty().WithMessage("InstrumentSymbol is required")
            .NotNull()
            .Length(1, 10).WithMessage("InstrumentSymbol must be between 1 and 10 characters")
            .Matches(@"^[A-Z]+$").WithMessage("InstrumentSymbol must contain only uppercase letters");

        RuleFor(x => x.OrderType)
            .NotNull().WithMessage("OrderType is required")
            .IsInEnum().WithMessage("Invalid OrderType");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Quantity cannot exceed 1,000,000");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price cannot exceed 999,999.99");
    }
}