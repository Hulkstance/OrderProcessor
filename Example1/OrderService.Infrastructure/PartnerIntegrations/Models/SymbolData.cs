namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed class SymbolData
{
    public required string BaseAsset { get; init; }
    public required string QuoteAsset { get; init; }
    public string? Name { get; init; }
    public required bool Trading { get; init; }
    public decimal? MaxTradeQuantity { get; init; }
    public decimal? MinTradeQuantity { get; init; }
    public decimal? MinNotionalValue { get; init; }
    public decimal? QuantityStep { get; init; }
    public decimal? PriceStep { get; init; }
    public int? QuantityDecimals { get; init; }
    public int? PriceDecimals { get; init; }
    public int? PriceSignificantFigures { get; init; }
}
