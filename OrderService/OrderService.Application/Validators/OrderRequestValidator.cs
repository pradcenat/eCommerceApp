using FluentValidation;
using OrderService.Application.RequestResponse;

namespace OrderService.Application.Validators
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.UserEmail)
                .NotEmpty().WithMessage("User email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("Shipping address is required")
                .MaximumLength(500).WithMessage("Shipping address cannot exceed 500 characters");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Order must have at least one item");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId)
                    .NotEmpty().WithMessage("Product ID is required");

                item.RuleFor(x => x.ProductName)
                    .NotEmpty().WithMessage("Product name is required");

                item.RuleFor(x => x.UnitPrice)
                    .GreaterThan(0).WithMessage("Unit price must be greater than 0");

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0");
            });
        }
    }
}
