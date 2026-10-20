using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Orders;

namespace OrderService.Application.Orders.Queries;

public record GetOrdersQuery(
    OrderStatus? Status = null,
    string? Exchange = null,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<ErrorOr<GetOrdersResponse>>;

public record GetOrdersResponse(
    List<OrderDto> Orders,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record OrderDto(
    Guid Id,
    string Exchange,
    string Symbol,
    OrderSide Side,
    OrderType OrderType,
    decimal Quantity,
    decimal? Price,
    OrderStatus Status,
    string? ExternalId,
    decimal FilledQuantity,
    decimal RemainingQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public class GetOrdersQueryHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetOrdersQuery, ErrorOr<GetOrdersResponse>>
{
    public async Task<ErrorOr<GetOrdersResponse>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
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

        if (accountIds.Count == 0)
            return new GetOrdersResponse([], 0, request.PageNumber, request.PageSize);

        var query = dbContext.Orders
            .Include(o => o.Fills)
            .Where(o => accountIds.Contains(o.AccountId));

        if (request.Status.HasValue)
            query = query.Where(o => o.State.Value == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Exchange))
            query = query.Where(o => o.Exchange == request.Exchange);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderDto(
                o.Id,
                o.Exchange,
                o.SymbolId,
                o.Side,
                o.OrderType,
                o.Quantity,
                o.Price,
                o.State.Value,
                o.ExternalId,
                o.Fills.Sum(f => f.FillQuantity),
                o.Quantity - o.Fills.Sum(f => f.FillQuantity),
                o.CreatedAt,
                o.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
        
        return new GetOrdersResponse(
            orders,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
