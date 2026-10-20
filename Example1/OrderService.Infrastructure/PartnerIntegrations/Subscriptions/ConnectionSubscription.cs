using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;

namespace OrderService.Infrastructure.PartnerIntegrations.Subscriptions;

internal sealed class ConnectionSubscription
{
    public string SymbolId { get; set; }
    public Action<SubscriptionEvent> StatusCallback { get; set; }
    public Action<DataEvent<SharedSpotTicker>> DataCallback { get; set; }

    public ConnectionSubscription(string symbolId, Action<SubscriptionEvent> statusCallback, Action<DataEvent<SharedSpotTicker>> dataCallback)
    {
        SymbolId = symbolId;
        StatusCallback = statusCallback;
        DataCallback = dataCallback;
    }
}
