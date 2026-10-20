using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Users;

namespace OrderService.Infrastructure.Persistence.Configurations.Users;

public class UserConfiguration : AuditableConfiguration<User>
{
    public override void PostConfigure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(User.ExternalIdMaxLength);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(User.EmailMaxLength);

        builder.HasIndex(x => x.ExternalId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
