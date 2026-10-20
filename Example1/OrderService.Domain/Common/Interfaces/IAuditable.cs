namespace OrderService.Domain.Common.Interfaces;

public interface IAuditable
{
    DateTime CreatedAt { get; }
    string CreatedBy { get; }
    DateTime? UpdatedAt { get; }
    string? UpdatedBy { get; }
    
    bool IsActive { get; }

    void SetCreated(TimeProvider timeProvider, string? createdBy = null);
    void SetUpdated(TimeProvider timeProvider, string? updatedBy = null);
    
    void Activate();
    void Deactivate();
}
