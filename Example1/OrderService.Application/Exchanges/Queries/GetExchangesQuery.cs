using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Exchanges.Queries;

public enum OrderDirection { Ascending, Descending }

public record GetExchangesQuery(
    string? Query = null,
    string? OrderBy = null,
    OrderDirection? OrderDirection = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<ErrorOr<GetExchangesResponse>>;

public record GetExchangesResponse(
    int Page,
    int PageSize,
    int Total,
    List<ExchangeDto> Exchanges
);

public record ExchangeDto(
    string Exchange,
    decimal UsdVolume,
    int Symbols
);

public class GetExchangesQueryHandler(IAppDbContext dbContext, IConfiguration configuration)
    : IRequestHandler<GetExchangesQuery, ErrorOr<GetExchangesResponse>>
{
    private readonly string[]? _enabledExchanges = configuration["EnabledExchanges"]?.Split(';', StringSplitOptions.RemoveEmptyEntries);

    public async Task<ErrorOr<GetExchangesResponse>> Handle(GetExchangesQuery request, CancellationToken cancellationToken)
    {
        var dbQuery = dbContext.ExchangeSymbols.GroupBy(x => x.Exchange);

        if (_enabledExchanges != null)
            dbQuery = dbQuery.Where(x => _enabledExchanges.Contains(x.Key));

        if (!string.IsNullOrEmpty(request.Query))
            dbQuery = dbQuery.Where(x => x.Key.Contains(request.Query));

        var projection = dbQuery.Select(x => new
        {
            Exchange = x.Key,
            Symbols  = x.Where(s => s.Enabled == true).Count(),
            UsdVolume = x.Sum(s => s.UsdVolume ?? 0)
        });

        projection = request.OrderBy switch
        {
            nameof(ExchangeDto.Symbols) when request.OrderDirection == Queries.OrderDirection.Ascending
                => projection.OrderBy(x => x.Symbols),
            nameof(ExchangeDto.Symbols)
                => projection.OrderByDescending(x => x.Symbols),
            _ when request.OrderDirection == Queries.OrderDirection.Ascending
                => projection.OrderBy(x => x.UsdVolume),
            _ => projection.OrderByDescending(x => x.UsdVolume)
        };

        var total = await projection.CountAsync(cancellationToken);
        var result = await projection
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new GetExchangesResponse(
            request.Page,
            request.PageSize,
            total,
            result.Select(x => new ExchangeDto(x.Exchange, x.UsdVolume, x.Symbols)).ToList());
    }
}
