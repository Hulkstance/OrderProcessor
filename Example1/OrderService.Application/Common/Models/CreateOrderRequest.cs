using OrderService.Domain.Accounts;
using OrderService.Domain.Orders;

namespace OrderService.Application.Common.Models;

public class CreateOrderRequest
{
    public required Account Account { get; init; }
    public required string BaseAsset { get; init; }
    public required string QuoteAsset { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType OrderType { get; init; }
    public required string ClientOrderId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? Price { get; init; }
    public TimeInForce? TimeInForce { get; init; }
}
