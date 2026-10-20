using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Orders;

namespace OrderService.Infrastructure.Persistence.Configurations.Orders;

public class OrderConfiguration : AuditableConfiguration<Order>
{
    public override void PostConfigure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.Exchange).HasMaxLength(Order.ExchangeMaxLength).IsRequired();
        builder.Property(x => x.SymbolId).HasMaxLength(Order.SymbolIdMaxLength).IsRequired();
        builder.Property(x => x.Side).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.OrderType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.TimeInForce).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Quantity).IsRequired().HasPrecision(18, 8);
        builder.Property(x => x.Price).HasPrecision(18, 8);
        builder.OwnsOne(x => x.State)
            .Property(s => s.Value)
            .HasColumnName("State")
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<OrderStatus>(v))
            .HasMaxLength(50);
        builder.Property(x => x.ExecutedQuantity).IsRequired().HasPrecision(18, 8);
        builder.Property(x => x.AveragePrice).HasPrecision(18, 8);
        builder.Property(x => x.ExternalId).HasMaxLength(Order.ExternalIdMaxLength);
        builder.Property(x => x.InternalId).IsRequired().HasMaxLength(Order.InternalIdMaxLength);

        builder.HasOne(o => o.Account)
            .WithMany(a => a.Orders)
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(o => o.Fills)
            .WithOne(f => f.Order)
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => x.ExternalId).IsUnique();
        builder.HasIndex(x => x.InternalId).IsUnique();
        builder.HasIndex(x => x.AccountId);
    }
}
