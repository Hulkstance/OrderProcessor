using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Symbols;

namespace OrderService.Infrastructure.Persistence.Configurations.Symbols;

public class ExchangeAssetConfiguration : IEntityTypeConfiguration<ExchangeAsset>
{
    public void Configure(EntityTypeBuilder<ExchangeAsset> builder)
    {
        builder.ToTable("ExchangeAssets", SchemaNames.Import);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(ExchangeAsset.IdMaxLength).IsRequired();

        builder.Property(x => x.Exchange).HasMaxLength(ExchangeAsset.ExchangeMaxLength).IsRequired();
        builder.Property(x => x.Asset).HasMaxLength(ExchangeAsset.AssetMaxLength).IsRequired();
        builder.Property(x => x.AssetType).IsRequired().HasConversion<string>();
        builder.Property(x => x.TickerType).HasDefaultValue(Domain.Symbols.TickerType.Day24H).HasConversion<string>();
        builder.Property(x => x.Value).HasPrecision(28, 8);
        builder.Property(x => x.Volume).HasPrecision(28, 8);
        builder.Property(x => x.ChangePercentage).HasPrecision(12, 4);
        builder.Property(x => x.LastSyncedAt).IsRequired();

        builder.HasIndex(x => x.Exchange);
    }
}
