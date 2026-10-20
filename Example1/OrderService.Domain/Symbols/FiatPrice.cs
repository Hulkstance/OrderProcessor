namespace OrderService.Domain.Symbols;

public class FiatPrice
{
    public const int IdMaxLength = 10;

    private FiatPrice() { } // EF Core

    public string Id { get; private set; } = null!;
    public decimal Price { get; private set; }
    public DateTime LastSyncedAt { get; private set; }

    public static FiatPrice Create(string currency, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new FiatPrice
        {
            Id = currency,
            Price = price,
            LastSyncedAt = DateTime.UtcNow
        };
    }
}
