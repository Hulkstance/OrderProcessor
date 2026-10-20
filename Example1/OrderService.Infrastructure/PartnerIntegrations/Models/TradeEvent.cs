using CryptoExchange.Net.SharedApis;

namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed record TradeEvent(
    Guid             AccountId,
    string           Exchange,
    string           Symbol,
    string           OrderId,
    string           TradeId,
    decimal          Price,
    decimal          Quantity,
    DateTime         Timestamp,
    SharedOrderSide? Side     = null,
    decimal?         Fee      = null,
    string?          FeeAsset = null,
    SharedRole?      Role     = null);
