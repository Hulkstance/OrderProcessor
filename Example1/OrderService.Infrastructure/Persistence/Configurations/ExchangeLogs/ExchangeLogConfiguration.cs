using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.ExchangeLogs;
using OrderService.Domain.Orders;

namespace OrderService.Infrastructure.Persistence.Configurations.ExchangeLogs;

public class ExchangeLogConfiguration : AuditableConfiguration<ExchangeLog>
{
    public override void PostConfigure(EntityTypeBuilder<ExchangeLog> builder)
    {
        builder.ToTable("ExchangeLogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.ExchangeOrderId).HasMaxLength(Order.ExternalIdMaxLength).IsRequired(false);
        builder.Property(x => x.ClientOrderId).HasMaxLength(Order.InternalIdMaxLength).IsRequired(false);
        builder.Property(x => x.MessageType).HasMaxLength(ExchangeLog.MessageTypeMaxLength).IsRequired();
        builder.Property(x => x.RequestPayload).IsRequired(false);
        builder.Property(x => x.ResponsePayload).IsRequired(false);
        builder.Property(x => x.ErrorText).IsRequired(false);
        builder.Property(x => x.IsSuccess).IsRequired();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => x.ExchangeOrderId).HasFilter("[ExchangeOrderId] IS NOT NULL");
        builder.HasIndex(x => x.ClientOrderId).HasFilter("[ClientOrderId] IS NOT NULL");
        builder.HasIndex(x => new { x.AccountId, x.CreatedAt });
        builder.HasIndex(x => new { x.AccountId, x.MessageType });
    }
}
