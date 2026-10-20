using OrderService.Domain.Accounts;

namespace OrderService.Application.Common.Models;

public class CancelOrderRequest
{
    public required Account Account { get; init; }
    public required string BaseAsset { get; init; }
    public required string QuoteAsset { get; init; }
    public required string ClientOrderId { get; init; }
}
