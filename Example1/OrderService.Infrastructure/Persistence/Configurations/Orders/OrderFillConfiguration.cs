using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Orders;

namespace OrderService.Infrastructure.Persistence.Configurations.Orders;

public class OrderFillConfiguration : AuditableConfiguration<OrderFill>
{
    public override void PostConfigure(EntityTypeBuilder<OrderFill> builder)
    {
        builder.ToTable("OrderFills");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(OrderFill.ExchangeTradeIdMaxLength);
        builder.Property(x => x.FillPrice).IsRequired().HasPrecision(18, 8);
        builder.Property(x => x.FillQuantity).IsRequired().HasPrecision(18, 8);
        builder.Property(x => x.FeeAmount).HasPrecision(18, 8);
        builder.Property(x => x.FeeAsset).HasMaxLength(OrderFill.FeeAssetMaxLength);
        builder.Property(x => x.FilledAt).IsRequired();
        
        builder.HasIndex(x => x.ExternalId).IsUnique();
        builder.HasIndex(x => x.OrderId);
        
        builder.HasOne(f => f.Order)
            .WithMany(o => o.Fills)
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
