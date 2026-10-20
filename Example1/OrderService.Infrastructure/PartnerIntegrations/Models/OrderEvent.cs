using CryptoExchange.Net.SharedApis;

namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed record OrderEvent(
    Guid              AccountId,
    string            Exchange,
    string            ExchangeOrderId,
    string            ClientOrderId,
    SharedOrderStatus Status,
    decimal?          QuantityFilledBase,
    decimal?          QuantityFilledQuote,
    decimal?          AveragePrice,
    DateTime          UpdateTime);
