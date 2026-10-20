namespace OrderService.Domain.Symbols;

public class ExchangeAsset
{
    public const int ExchangeMaxLength = 50;
    public const int AssetMaxLength = 20;
    public const int IdMaxLength = ExchangeMaxLength + 1 + AssetMaxLength; // "{exchange}-{asset}"

    private ExchangeAsset() { } // EF Core

    private ExchangeAsset(string id, string exchange, string asset, AssetType assetType)
    {
        Id = id;
        Exchange = exchange;
        Asset = asset;
        AssetType = assetType;
        LastSyncedAt = DateTime.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string Exchange { get; private set; } = null!;
    public string Asset { get; private set; } = null!;
    public AssetType AssetType { get; private set; }
    public decimal? Value { get; private set; }
    public decimal? Volume { get; private set; }
    public decimal? ChangePercentage { get; private set; }
    public TickerType TickerType { get; private set; }
    public DateTime LastSyncedAt { get; private set; }

    public static ExchangeAsset Create(string exchange, string asset, AssetType assetType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(asset);

        return new ExchangeAsset(
            id: $"{exchange}-{asset}",
            exchange: exchange,
            asset: asset,
            assetType: assetType);
    }
    
    public void UpdateTicker(decimal? value, decimal? volume, decimal? changePercentage, TickerType tickerType)
    {
        Value = value;
        Volume = volume;
        ChangePercentage = changePercentage;
        TickerType = tickerType;
        LastSyncedAt = DateTime.UtcNow;
    }
}
