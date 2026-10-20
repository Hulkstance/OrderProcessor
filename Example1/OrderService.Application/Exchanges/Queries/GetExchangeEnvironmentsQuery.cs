using ErrorOr;
using MediatR;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Exchanges.Queries;

public record GetExchangeEnvironmentsQuery(string Exchange) : IRequest<ErrorOr<string[]>>;

public class GetExchangeEnvironmentsQueryHandler(IExchangeInfoService exchangeInfoService)
    : IRequestHandler<GetExchangeEnvironmentsQuery, ErrorOr<string[]>>
{
    public Task<ErrorOr<string[]>> Handle(GetExchangeEnvironmentsQuery request, CancellationToken cancellationToken)
    {
        var info = exchangeInfoService.GetInfo(request.Exchange);

        if (info is null)
            return Task.FromResult<ErrorOr<string[]>>(
                Error.NotFound("Exchange.NotFound", $"Exchange '{request.Exchange}' was not found"));

        return Task.FromResult<ErrorOr<string[]>>(info.Environments);
    }
}
