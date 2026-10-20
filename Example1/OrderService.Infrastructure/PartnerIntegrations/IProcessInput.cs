namespace OrderService.Infrastructure.PartnerIntegrations;

internal interface IProcessInput<T>
{
    Task<PublishItem<T>?> ReadAsync(CancellationToken ct);
}
