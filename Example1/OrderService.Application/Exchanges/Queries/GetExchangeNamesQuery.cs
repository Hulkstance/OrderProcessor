using ErrorOr;
using MediatR;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Exchanges.Queries;

public record GetExchangeNamesQuery : IRequest<ErrorOr<string[]>>;

public class GetExchangeNamesQueryHandler(IExchangeInfoService exchangeInfoService)
    : IRequestHandler<GetExchangeNamesQuery, ErrorOr<string[]>>
{
    public Task<ErrorOr<string[]>> Handle(GetExchangeNamesQuery request, CancellationToken cancellationToken)
        => Task.FromResult<ErrorOr<string[]>>(exchangeInfoService.GetAllNames());
}
