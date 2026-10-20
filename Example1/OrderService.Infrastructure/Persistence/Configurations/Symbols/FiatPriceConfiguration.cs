using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Symbols;

namespace OrderService.Infrastructure.Persistence.Configurations.Symbols;

public class FiatPriceConfiguration : IEntityTypeConfiguration<FiatPrice>
{
    public void Configure(EntityTypeBuilder<FiatPrice> builder)
    {
        builder.ToTable("FiatPrices", SchemaNames.Import);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(FiatPrice.IdMaxLength).IsRequired();
        builder.Property(x => x.Price).HasPrecision(28, 8).IsRequired();
        builder.Property(x => x.LastSyncedAt).IsRequired();
    }
}
