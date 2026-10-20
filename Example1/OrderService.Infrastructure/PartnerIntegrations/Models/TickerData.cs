using OrderService.Domain.Symbols;

namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed record TickerData(
    string Exchange,
    string BaseAsset,
    string QuoteAsset,
    decimal? LastPrice,
    decimal? HighPrice,
    decimal? LowPrice,
    decimal? Volume,
    decimal? QuoteVolume,
    decimal? ChangePercentage,
    TickerType TickerType);
