namespace OrderService.Domain.Symbols;

public class ExchangeSymbol
{
    public const int ExchangeMaxLength = 50;
    public const int AssetMaxLength = 20;
    public const int NameMaxLength = 30;
    public const int IdMaxLength = ExchangeMaxLength + 1 + AssetMaxLength + 1 + AssetMaxLength; // "{exchange}-{base}-{quote}"

    private ExchangeSymbol() { } // EF Core

    private ExchangeSymbol(
        string id,
        string exchange,
        string? name,
        string baseAsset,
        string quoteAsset,
        AssetType baseAssetType,
        AssetType quoteAssetType,
        decimal? minTradeQuantity,
        decimal? minNotionalValue,
        decimal? quantityStep,
        decimal? priceStep,
        int? quantityDecimals,
        int? priceDecimals,
        int? priceSignificantFigures,
        bool? enabled,
        DateTime updateTime)
    {
        Id = id;
        BaseAssetExchangeId = $"{exchange}-{baseAsset}";
        QuoteAssetExchangeId = $"{exchange}-{quoteAsset}";
        Exchange = exchange;
        Name = name;
        BaseAsset = baseAsset;
        QuoteAsset = quoteAsset;
        BaseAssetType = baseAssetType;
        QuoteAssetType = quoteAssetType;
        MinTradeQuantity = minTradeQuantity;
        MinNotionalValue = minNotionalValue;
        QuantityStep = quantityStep;
        PriceStep = priceStep;
        QuantityDecimals = quantityDecimals;
        PriceDecimals = priceDecimals;
        PriceSignificantFigures = priceSignificantFigures;
        Enabled = enabled;
        LastSyncedAt = updateTime;
    }

    public string Id { get; private set; } = null!;
    public string Exchange { get; private set; } = null!;
    public string? Name { get; private set; }
    public string BaseAsset { get; private set; } = null!;
    public string QuoteAsset { get; private set; } = null!;
    public string BaseAssetExchangeId { get; private set; } = null!;
    public string QuoteAssetExchangeId { get; private set; } = null!;
    public AssetType BaseAssetType { get; private set; }
    public AssetType QuoteAssetType { get; private set; }
    public decimal? MinTradeQuantity { get; private set; }
    public decimal? MinNotionalValue { get; private set; }
    public decimal? QuantityStep { get; private set; }
    public decimal? PriceStep { get; private set; }
    public int? QuantityDecimals { get; private set; }
    public int? PriceDecimals { get; private set; }
    public int? PriceSignificantFigures { get; private set; }
    public bool? Enabled { get; private set; }
    public decimal? LastPrice { get; private set; }
    public decimal? HighPrice { get; private set; }
    public decimal? LowPrice { get; private set; }
    public decimal? Volume { get; private set; }
    public decimal? QuoteVolume { get; private set; }
    public decimal? UsdVolume { get; private set; }
    public decimal? ChangePercentage { get; private set; }
    public TickerType TickerType { get; private set; }
    public DateTime LastSyncedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static ExchangeSymbol Create(
        string exchange,
        string baseAsset,
        string quoteAsset,
        AssetType baseAssetType,
        AssetType quoteAssetType,
        string? name = null,
        decimal? minTradeQuantity = null,
        decimal? minNotionalValue = null,
        decimal? quantityStep = null,
        decimal? priceStep = null,
        int? quantityDecimals = null,
        int? priceDecimals = null,
        int? priceSignificantFigures = null,
        bool? enabled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAsset);
        ArgumentException.ThrowIfNullOrWhiteSpace(quoteAsset);

        return new ExchangeSymbol(
            id: $"{exchange}-{baseAsset}-{quoteAsset}",
            exchange: exchange,
            name: name,
            baseAsset: baseAsset,
            quoteAsset: quoteAsset,
            baseAssetType: baseAssetType,
            quoteAssetType: quoteAssetType,
            minTradeQuantity: minTradeQuantity,
            minNotionalValue: minNotionalValue,
            quantityStep: quantityStep,
            priceStep: priceStep,
            quantityDecimals: quantityDecimals,
            priceDecimals: priceDecimals,
            priceSignificantFigures: priceSignificantFigures,
            enabled: enabled,
            updateTime: DateTime.UtcNow);
    }

    public static ExchangeSymbol ForRemoval(string id, string exchange)
    {
        var s = new ExchangeSymbol();
        s.Id = id;
        s.Exchange = exchange;
        s.LastPrice = null;
        s.HighPrice = null;
        s.LowPrice = null;
        s.Volume = 0;
        s.QuoteVolume = 0;
        s.ChangePercentage = null;
        s.Enabled = false;
        s.LastSyncedAt = DateTime.UtcNow;
        s.DeletedAt = DateTime.UtcNow;
        return s;
    }

    public static ExchangeSymbol ForTickerUpdate(
        string id,
        decimal? lastPrice,
        decimal? highPrice,
        decimal? lowPrice,
        decimal? volume,
        decimal? quoteVolume,
        decimal? changePercentage,
        TickerType tickerType)
    {
        var s = new ExchangeSymbol();
        s.Id = id;
        s.LastPrice = lastPrice;
        s.HighPrice = highPrice;
        s.LowPrice = lowPrice;
        s.Volume = volume;
        s.QuoteVolume = quoteVolume;
        s.ChangePercentage = changePercentage;
        s.TickerType = tickerType;
        s.LastSyncedAt = DateTime.UtcNow;
        return s;
    }

    public static ExchangeSymbol ForUsdVolumeUpdate(string id, decimal? usdVolume)
    {
        var s = new ExchangeSymbol();
        s.Id = id;
        s.UsdVolume = usdVolume;
        s.LastSyncedAt = DateTime.UtcNow;
        return s;
    }

    public void MarkDeleted()
    {
        Enabled = false;
        DeletedAt = DateTime.UtcNow;
    }
}
