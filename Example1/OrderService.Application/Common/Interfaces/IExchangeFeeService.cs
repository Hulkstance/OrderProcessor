using ErrorOr;

namespace OrderService.Application.Common.Interfaces;

public record ExchangeFeesDto(string Exchange, decimal? MakerFee, decimal? TakerFee);

public interface IExchangeFeeService
{
    Task<ErrorOr<ExchangeFeesDto>> GetFeesAsync(Guid accountId, string exchange, string symbolId, CancellationToken ct);
}
