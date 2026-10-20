using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Symbols;

namespace OrderService.Infrastructure.Persistence.Configurations.Symbols;

public class ExchangeSymbolConfiguration : IEntityTypeConfiguration<ExchangeSymbol>
{
    public void Configure(EntityTypeBuilder<ExchangeSymbol> builder)
    {
        builder.ToTable("ExchangeSymbols", SchemaNames.Import);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(ExchangeSymbol.IdMaxLength).IsRequired();

        builder.Property(x => x.Exchange).HasMaxLength(ExchangeSymbol.ExchangeMaxLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(ExchangeSymbol.NameMaxLength);
        builder.Property(x => x.BaseAsset).HasMaxLength(ExchangeSymbol.AssetMaxLength).IsRequired();
        builder.Property(x => x.QuoteAsset).HasMaxLength(ExchangeSymbol.AssetMaxLength).IsRequired();
        builder.Property(x => x.BaseAssetExchangeId).HasMaxLength(ExchangeAsset.IdMaxLength).IsRequired();
        builder.Property(x => x.QuoteAssetExchangeId).HasMaxLength(ExchangeAsset.IdMaxLength).IsRequired();

        builder.Property(x => x.BaseAssetType).IsRequired().HasConversion<string>();
        builder.Property(x => x.QuoteAssetType).IsRequired().HasConversion<string>();
        builder.Property(x => x.Enabled);
        builder.Property(x => x.TickerType).HasDefaultValue(TickerType.Day24H).HasConversion<string>();
        builder.Property(x => x.LastPrice).HasPrecision(28, 8);
        builder.Property(x => x.HighPrice).HasPrecision(28, 8);
        builder.Property(x => x.LowPrice).HasPrecision(28, 8);
        builder.Property(x => x.Volume).HasPrecision(28, 8);
        builder.Property(x => x.QuoteVolume).HasPrecision(28, 8);
        builder.Property(x => x.UsdVolume).HasPrecision(28, 8);
        builder.Property(x => x.ChangePercentage).HasPrecision(12, 4);
        builder.Property(x => x.LastSyncedAt).IsRequired();

        builder.HasIndex(x => x.Exchange);
        builder.HasIndex(x => new { x.Exchange, x.Enabled });
        builder.HasIndex(x => x.BaseAssetExchangeId);
        builder.HasIndex(x => x.QuoteAssetExchangeId);
    }
}
