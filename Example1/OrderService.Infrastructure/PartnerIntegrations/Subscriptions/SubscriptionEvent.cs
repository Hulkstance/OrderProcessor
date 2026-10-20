namespace OrderService.Infrastructure.PartnerIntegrations.Subscriptions;

internal sealed class SubscriptionEvent
{
    public string? Exchange { get; set; }
    public StreamStatus Status { get; set; }

    public SubscriptionEvent(StreamStatus status)
    {
        Status = status;
    }
}

internal enum StreamStatus
{
    Interrupted,
    Restored,
    Failed
}
