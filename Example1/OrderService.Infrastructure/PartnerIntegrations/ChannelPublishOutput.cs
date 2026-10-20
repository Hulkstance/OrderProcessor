using System.Threading.Channels;

namespace OrderService.Infrastructure.PartnerIntegrations;

internal sealed class ChannelPublishOutput<T>(ChannelWriter<PublishItem<T>> writer) : IPublishOutput<T>
{
    public async Task PublishAsync(PublishItem<T> item)
        => await writer.WriteAsync(item);
}
