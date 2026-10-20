using OrderService.Domain.Common.Base;
using OrderService.Domain.Orders;
using OrderService.Domain.Users;

namespace OrderService.Domain.Accounts;

public class Account : AggregateRoot<Guid>
{
    public const int ExchangeMaxLength = 50;
    public const int EnvironmentMaxLength = 20;

    private readonly List<Order> _orders = [];
    
    private Account() { } // EF Core

    private Account(
        Guid userId,
        string environment,
        string exchange,
        string apiKey,
        string apiSecret,
        string? passphrase)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Environment = environment;
        Exchange = exchange;
        ApiKey = apiKey;
        ApiSecret = apiSecret;
        Passphrase = passphrase;
        IsActive = true;
    }
    
    public Guid UserId { get; private set; }
    public string Environment { get; private set; } = null!;
    public string Exchange { get; private set; } = null!;

    // Azure AD B2C ID reference
    public int InternalId { get; private set; }
    
    public string ApiKey
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ApiKey));
            field = value;
        }
    } = null!;
    
    public string ApiSecret
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ApiSecret));
            field = value;
        }
    } = null!;
    
    public string? Passphrase { get; private set; }
    
    public User User { get; private set; } = null!;
    public IReadOnlyList<Order> Orders => _orders.AsReadOnly();
    
    public static Account Create(
        Guid userId,
        string environment,
        string exchange,
        string apiKey,
        string apiSecret,
        string? passphrase = null)
    {
        return new Account(
            userId,
            environment,
            exchange,
            apiKey,
            apiSecret,
            passphrase);
    }
}
