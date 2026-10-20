using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Orders;

namespace OrderService.Application.Orders.Commands;

public record CreateOrderCommand(
    int AccountId,
    string SymbolId,
    OrderSide OrderSide,
    OrderType OrderType,
    decimal Quantity,
    decimal? Price = null,
    TimeInForce? TimeInForce = null
) : IRequest<ErrorOr<CreateOrderResponse>>;

public record CreateOrderResponse(string InternalId);

public class CreateOrderCommandHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext)
    : IRequestHandler<CreateOrderCommand, ErrorOr<CreateOrderResponse>>
{
    public async Task<ErrorOr<CreateOrderResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var symbolData = request.SymbolId.Split("-");
        var account = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                    x.InternalId == request.AccountId &&
                    x.User.ExternalId == userId.ToString() &&
                    x.IsActive &&
                    x.Exchange == symbolData[0],
                cancellationToken);

        if (account is null)
        {
            return Error.NotFound(
                code: "Account.NotFound",
                description: "No API key configured for exchange");
        }

        var orderIdResult = await GenerateUniqueOrderId(account.Id, cancellationToken);
        if (orderIdResult.IsError)
        {
            return orderIdResult.Errors;
        }

        var order = Order.Create(
            accountId: account.Id,
            exchange: account.Exchange,
            symbolId: request.SymbolId,
            side: request.OrderSide,
            orderType: request.OrderType,
            internalId: orderIdResult.Value,
            quantity: request.Quantity,
            price: request.Price,
            timeInForce: request.TimeInForce);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse(order.InternalId);
    }

    private async Task<ErrorOr<string>> GenerateUniqueOrderId(Guid accountId, CancellationToken ct)
    {
        for (var i = 0; i < 5; i++)
        {
            var id = $"QS-{UniqueIdGenerator.Generate(17)}";

            var exists = await dbContext.Orders
                .AsNoTracking()
                .AnyAsync(x => x.InternalId == id && x.AccountId == accountId, ct);

            if (!exists)
                return id;
        }

        return Error.Conflict(
            code: "Order.InternalIdCollision",
            description: "You got unlucky while generating order id. Please retry.");
    }
}
