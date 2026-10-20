using FluentValidation;
using OrderService.Domain.Orders;

namespace OrderService.Application.Orders.Commands;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0)
            .WithMessage("Account ID is required");

        RuleFor(x => x.SymbolId)
            .NotEmpty()
            .WithMessage("SymbolId is required")
            .Must(BeValidSymbolId)
            .WithMessage("SymbolId must be in the format {exchange}-{baseAsset}-{quoteAsset} (e.g. binance-BTC-USDC)");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.Price.HasValue)
            .WithMessage("Price must be greater than zero when specified");

        RuleFor(x => x.Price)
            .NotNull()
            .When(x => x.OrderType == OrderType.Limit)
            .WithMessage("Price is required for limit orders");

        RuleFor(x => x.Price)
            .Null()
            .When(x => x.OrderType == OrderType.Market)
            .WithMessage("Price must not be specified for market orders");

        RuleFor(x => x.TimeInForce)
            .NotNull()
            .When(x => x.OrderType == OrderType.Limit)
            .WithMessage("TimeInForce is required for limit orders");

        RuleFor(x => x.TimeInForce)
            .Null()
            .When(x => x.OrderType == OrderType.Market)
            .WithMessage("TimeInForce must not be specified for market orders");
    }

    private static bool BeValidSymbolId(string symbolId)
    {
        var parts = symbolId.Split('-');
        return parts.Length == 3 && parts[0].Length > 0 && parts[1].Length > 0 && parts[2].Length > 0;
    }
}
