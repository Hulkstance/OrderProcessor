using OrderService.Domain.Accounts;
using OrderService.Domain.Common.Base;

namespace OrderService.Domain.Users;

public class User : AggregateRoot<Guid>
{
    public const int EmailMaxLength = 255;
    public const int ExternalIdMaxLength = 255;

    private readonly List<Account> _accounts = [];
    
    private User() { } // EF Core

    private User(string externalId, string email)
    {
        Id = Guid.NewGuid();
        ExternalId = externalId;
        Email = email;
        IsActive = true;
    }

    public string ExternalId
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ExternalId));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, ExternalIdMaxLength, nameof(ExternalId));
            field = value;
        }
    } = null!;

    public string Email
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Email));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, EmailMaxLength, nameof(Email));
            field = value;
        }
    } = null!;
    
    public IReadOnlyList<Account> Accounts => _accounts.AsReadOnly();

    public static User Create(string externalId, string email)
    {
        return new User(externalId, email);
    }
}
