using OrderService.Domain.Common.Interfaces;

namespace OrderService.Domain.Common.Base;

/// <summary>
/// Tracks creation and modification of objects.
/// </summary>
public abstract class Auditable : IAuditable
{
    public const int CreatedByMaxLength = 128;
    public const int UpdatedByMaxLength = 128;

    private const string SystemUser = "System";

    public DateTime CreatedAt { get; private set; }

    public string CreatedBy
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(CreatedBy));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, CreatedByMaxLength, nameof(CreatedBy));
            field = value;
        }
    } = null!;

    public DateTime? UpdatedAt { get; private set; }

    public string? UpdatedBy
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(UpdatedBy));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, UpdatedByMaxLength, nameof(UpdatedBy));
            field = value;
        }
    }

    public bool IsActive { get; protected set; }
    
    public void SetCreated(TimeProvider timeProvider, string? createdBy)
    {
        CreatedAt = timeProvider.GetUtcNow().UtcDateTime;
        CreatedBy = createdBy ?? SystemUser;
    }

    public void SetUpdated(TimeProvider timeProvider, string? updatedBy)
    {
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        UpdatedBy = updatedBy ?? SystemUser;
    }
    
    public virtual void Activate()
    {
        IsActive = true;
    }
    
    public virtual void Deactivate()
    {
        IsActive = false;
    }
}
