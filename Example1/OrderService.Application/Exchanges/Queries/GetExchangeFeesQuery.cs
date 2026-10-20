using ErrorOr;
using MediatR;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Exchanges.Queries;

public record GetExchangeFeesQuery(Guid AccountId, string Exchange, string SymbolId)
    : IRequest<ErrorOr<GetExchangeFeesResponse>>;

public record GetExchangeFeesResponse(string Exchange, decimal? MakerFee, decimal? TakerFee);

public class GetExchangeFeesQueryHandler(IExchangeFeeService feeService)
    : IRequestHandler<GetExchangeFeesQuery, ErrorOr<GetExchangeFeesResponse>>
{
    public async Task<ErrorOr<GetExchangeFeesResponse>> Handle(GetExchangeFeesQuery request, CancellationToken cancellationToken)
    {
        var result = await feeService.GetFeesAsync(request.AccountId, request.Exchange, request.SymbolId, cancellationToken);

        if (result.IsError)
            return result.Errors;

        return new GetExchangeFeesResponse(result.Value.Exchange, result.Value.MakerFee, result.Value.TakerFee);
    }
}
