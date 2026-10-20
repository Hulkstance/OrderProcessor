using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Accounts;
using OrderService.Infrastructure.Persistence.Extensions;

namespace OrderService.Infrastructure.Persistence.Configurations.Accounts;

public class AccountConfiguration : AuditableConfiguration<Account>
{
    public override void PostConfigure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(Account.EnvironmentMaxLength).IsRequired();
        builder.Property(x => x.Exchange).HasMaxLength(Account.ExchangeMaxLength).IsRequired();
        
        builder.Property(x => x.InternalId)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn(seed: 1000, increment: 1);
        
        builder.Property(x => x.ApiKey).IsRequired();
        builder.Property(x => x.ApiSecret).IsRequired().Encrypt();
        builder.Property(x => x.Passphrase);

        builder.HasOne(uea => uea.User)
            .WithMany(u  => u.Accounts)
            .HasForeignKey(uea => uea.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // One account per user per exchange, just like in Bitsgap. Environment (live/testnet) shouldn't be considered
        builder
            .HasIndex(x => new { x.UserId, x.Exchange })
            .IsUnique();
    }
}
