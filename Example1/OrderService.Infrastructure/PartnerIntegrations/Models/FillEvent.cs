namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed record FillEvent(
    string   ExchangeTradeId,
    string   ExchangeOrderId,
    decimal  Price,
    decimal  Quantity,
    DateTime FilledAt,
    decimal? FeeAmount = null,
    string?  FeeAsset  = null);
