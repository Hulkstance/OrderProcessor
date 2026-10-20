using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Exchanges.Queries;

public record GetExchangeQuery(string Exchange) : IRequest<ErrorOr<GetExchangeResponse>>;

public record GetExchangeResponse(
    string Exchange,
    int Symbols,
    decimal UsdVolume,
    string? LogoUrl,
    string? Type,
    string? Url
);

public class GetExchangeQueryHandler(IAppDbContext dbContext, IExchangeInfoService exchangeInfoService)
    : IRequestHandler<GetExchangeQuery, ErrorOr<GetExchangeResponse>>
{
    public async Task<ErrorOr<GetExchangeResponse>> Handle(GetExchangeQuery request, CancellationToken cancellationToken)
    {
        var stats = await dbContext.ExchangeSymbols
            .Where(x => x.Exchange == request.Exchange)
            .GroupBy(x => x.Exchange)
            .Select(x => new
            {
                Symbols   = x.Where(s => s.Enabled == true).Count(),
                UsdVolume = x.Sum(s => s.UsdVolume ?? 0)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (stats is null)
            return Error.NotFound("Exchange.NotFound", $"Exchange '{request.Exchange}' was not found");

        var info = exchangeInfoService.GetInfo(request.Exchange);

        return new GetExchangeResponse(
            request.Exchange,
            stats.Symbols,
            stats.UsdVolume,
            info?.LogoUrl,
            info?.Type,
            info?.Url);
    }
}
