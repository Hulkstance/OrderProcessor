namespace OrderService.Infrastructure.PartnerIntegrations;

internal record PublishItem<T>
{
    public string? Exchange { get; set; }
    public IEnumerable<T> Data { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    public PublishItem(string? exchange = null)
    {
        Exchange = exchange;
        Timestamp = DateTime.UtcNow;
    }
}
