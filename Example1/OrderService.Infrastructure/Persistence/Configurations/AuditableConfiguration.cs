using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Common.Base;

namespace OrderService.Infrastructure.Persistence.Configurations;

public abstract class AuditableConfiguration<T> : IEntityTypeConfiguration<T>
    where T : Auditable
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(x => x.CreatedBy).HasMaxLength(Auditable.CreatedByMaxLength).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.UpdatedBy).HasMaxLength(Auditable.UpdatedByMaxLength).IsRequired(false);

        builder.Property(x => x.IsActive).IsRequired();
        
        PostConfigure(builder);
    }

    public abstract void PostConfigure(EntityTypeBuilder<T> builder);
}
