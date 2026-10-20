using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.SharedApis;
using CryptoExchange.Net.Trackers.UserData.Objects;

namespace OrderService.Infrastructure.PartnerIntegrations.Subscriptions;

internal sealed class AccountUpdateSubscription
{
    private readonly Lock _lock = new();
    private readonly List<AccountCallbacks> _callbacks;

    public AccountUpdateSubscription(
        Guid accountId,
        IExchangeSocketClient socketClient,
        AccountCallbacks callbacks,
        CancellationTokenSource cts)
    {
        AccountId = accountId;
        SocketClient = socketClient;
        Cts = cts;
        _callbacks = [callbacks];
    }

    public Guid AccountId { get; }
    public IExchangeSocketClient SocketClient { get; }
    public CancellationTokenSource Cts { get; }

    public List<string> ConnectionIds
    {
        get
        {
            lock (_lock)
                return _callbacks.Select(x => x.ConnectionId).ToList();
        }
    }

    public int CallbackCount
    {
        get
        {
            lock (_lock)
                return _callbacks.Count;
        }
    }

    public void AddCallback(AccountCallbacks callback)
    {
        lock (_lock)
            _callbacks.Add(callback);
    }

    public void Remove(string connectionId)
    {
        lock (_lock)
            _callbacks.Remove(_callbacks.Single(x => x.ConnectionId == connectionId));
    }

    public void Invoke(UserDataUpdate<SharedBalance[]> update)
    {
        lock (_lock)
        {
            foreach (var cb in _callbacks)
                cb.BalanceHandler(update);
        }
    }

    public void Invoke(UserDataUpdate<SharedSpotOrder[]> update)
    {
        lock (_lock)
        {
            foreach (var cb in _callbacks) 
                cb.OrderHandler(update);
        }
    }

    public void Invoke(UserDataUpdate<SharedUserTrade[]> update)
    {
        lock (_lock)
        {
            foreach (var cb in _callbacks)
                cb.TradeHandler(update);
        }
    }

    public void Invoke(SubscriptionEvent evnt)
    {
        lock (_lock)
        {
            foreach (var cb in _callbacks)
                cb.StatusHandler(evnt);
        }
    }
}

internal sealed class AccountCallbacks
{
    public AccountCallbacks(string connectionId,
        Action<UserDataUpdate<SharedBalance[]>> balanceHandler,
        Action<UserDataUpdate<SharedSpotOrder[]>> orderHandler,
        Action<UserDataUpdate<SharedUserTrade[]>> tradeHandler,
        Action<SubscriptionEvent> statusHandler)
    {
        ConnectionId = connectionId;
        BalanceHandler = balanceHandler;
        OrderHandler = orderHandler;
        TradeHandler = tradeHandler;
        StatusHandler = statusHandler;
    }

    public string ConnectionId { get; set; }
    public Action<UserDataUpdate<SharedBalance[]>> BalanceHandler { get; set; }
    public Action<UserDataUpdate<SharedSpotOrder[]>> OrderHandler { get; set; }
    public Action<UserDataUpdate<SharedUserTrade[]>> TradeHandler { get; set; }
    public Action<SubscriptionEvent> StatusHandler { get; set; }
}
