using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Orders.Commands;

public record CancelOrderCommand(string ClientOrderId) : IRequest<ErrorOr<CancelOrderResponse>>;

public record CancelOrderResponse(string ClientOrderId, bool Success, string Message);

public class CancelOrderCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<CancelOrderCommand, ErrorOr<CancelOrderResponse>>
{
    public async Task<ErrorOr<CancelOrderResponse>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == userId.ToString() && u.IsActive, cancellationToken);

        if (user is null)
            return Error.Unauthorized(description: "User not found");

        var accountIds = await dbContext.Accounts
            .Where(a => a.UserId == user.Id && a.IsActive)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var order = await dbContext.Orders
            .FirstOrDefaultAsync(
                o => o.InternalId == request.ClientOrderId && accountIds.Contains(o.AccountId),
                cancellationToken);

        if (order is null)
            return Error.NotFound(description: $"Order with ClientOrderId '{request.ClientOrderId}' not found");

        if (order.IsTerminalState())
            return Error.Validation(description: "Order is already in a terminal state and cannot be cancelled");

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CancelOrderResponse(order.InternalId, true, "Cancel request submitted");
    }
}
