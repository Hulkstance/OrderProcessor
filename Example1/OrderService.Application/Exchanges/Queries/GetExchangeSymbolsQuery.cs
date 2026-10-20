using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Exchanges.Queries;

public record GetExchangeSymbolsQuery(string ExchangeCode) : IRequest<ErrorOr<GetExchangeSymbolsResponse>>;

public record GetExchangeSymbolsResponse(List<SymbolDto> Symbols);

public record SymbolDto(
    string SymbolId,
    string BaseAsset,
    string QuoteAsset,
    AssetTypeDto BaseAssetType,
    AssetTypeDto QuoteAssetType,
    decimal? MinTradeQuantity,
    decimal? MinNotionalValue,
    decimal? QuantityStep,
    decimal? PriceStep,
    int? QuantityDecimals,
    int? PriceDecimals
);

public enum AssetTypeDto { Crypto, Stable, Fiat, LeveragedToken }

public class GetExchangeSymbolsQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetExchangeSymbolsQuery, ErrorOr<GetExchangeSymbolsResponse>>
{
    public async Task<ErrorOr<GetExchangeSymbolsResponse>> Handle(GetExchangeSymbolsQuery request, CancellationToken cancellationToken)
    {
        var symbols = await dbContext.ExchangeSymbols
            .Where(x => x.Exchange == request.ExchangeCode && x.Enabled == true && x.DeletedAt == null)
            .OrderBy(x => x.BaseAsset)
            .ThenBy(x => x.QuoteAsset)
            .Select(x => new SymbolDto(
                x.Id,
                x.BaseAsset,
                x.QuoteAsset,
                (AssetTypeDto)x.BaseAssetType,
                (AssetTypeDto)x.QuoteAssetType,
                x.MinTradeQuantity,
                x.MinNotionalValue,
                x.QuantityStep,
                x.PriceStep,
                x.QuantityDecimals,
                x.PriceDecimals))
            .ToListAsync(cancellationToken);

        return new GetExchangeSymbolsResponse(symbols);
    }
}
