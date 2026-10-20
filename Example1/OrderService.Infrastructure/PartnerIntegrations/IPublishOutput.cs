namespace OrderService.Infrastructure.PartnerIntegrations;

internal interface IPublishOutput<T>
{
    Task PublishAsync(PublishItem<T> item);
}
