using OrderService.Domain.Accounts;

namespace OrderService.Application.Common.Models;

public class GetOrdersRequest
{
    public required Account Account { get; init; }
    public string? BaseAsset { get; init; }
    public string? QuoteAsset { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public int? Limit { get; init; }
}
