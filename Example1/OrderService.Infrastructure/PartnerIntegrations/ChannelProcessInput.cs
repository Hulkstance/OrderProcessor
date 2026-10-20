using System.Threading.Channels;

namespace OrderService.Infrastructure.PartnerIntegrations;

internal sealed class ChannelProcessInput<T>(ChannelReader<PublishItem<T>> reader) : IProcessInput<T>
{
    public async Task<PublishItem<T>?> ReadAsync(CancellationToken ct)
        => await reader.ReadAsync(ct);
}
