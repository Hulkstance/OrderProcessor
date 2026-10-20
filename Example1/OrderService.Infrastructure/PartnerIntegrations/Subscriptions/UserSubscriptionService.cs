using System.Collections.Concurrent;
using CryptoClients.Net.Interfaces;
using CryptoClients.Net.Models;
using CryptoExchange.Net.SharedApis;
using CryptoExchange.Net.Trackers.UserData.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.PartnerIntegrations.Models;

namespace OrderService.Infrastructure.PartnerIntegrations.Subscriptions;

internal sealed class UserSubscriptionService(
    ILogger<UserSubscriptionService> logger,
    IConfiguration configuration,
    IExchangeUserClientProvider clientProvider,
    IExchangeTrackerFactory trackerFactory)
{
    private readonly string[]? _enabledExchanges = configuration.GetSection("EnabledExchanges").Get<string[]>();

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _userSemaphores = new();
    private readonly ConcurrentDictionary<Guid, AccountUpdateSubscription> _subscriptions = new();

    public async Task SubscribeAsync(
        string connectionId,
        Guid accountId,
        UserExchangeAuthentication auth,
        Action<UserDataUpdate<SharedBalance[]>> balanceHandler,
        Action<UserDataUpdate<SharedSpotOrder[]>> orderHandler,
        Action<UserDataUpdate<SharedUserTrade[]>> tradeHandler,
        Action<SubscriptionEvent> statusHandler,
        CancellationToken ct)
    {
        var semaphore = _userSemaphores.GetOrAdd(accountId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            logger.LogInformation("Connecting user subscriptions for account {AccountId}", accountId);

            if (_subscriptions.TryGetValue(accountId, out var existing))
            {
                logger.LogDebug("Account {AccountId} already connected, adding callback. Now {Count} subscription(s).", accountId, existing.CallbackCount + 1);
                existing.AddCallback(new AccountCallbacks(connectionId, balanceHandler, orderHandler, tradeHandler, statusHandler));
                return;
            }

            var exchangeName = auth.Exchange;

            if (_enabledExchanges?.Any() == true && !_enabledExchanges.Contains(exchangeName, StringComparer.InvariantCultureIgnoreCase))
            {
                logger.LogWarning("Account {AccountId} exchange {Exchange} is not in EnabledExchanges, skipping subscription", accountId, exchangeName);
                return;
            }

            var apiCredentials = ExchangeCredentials.CreateCredentialsForExchange(
                exchangeName,
                new DynamicCredentials(TradingMode.Spot, auth.ApiKey, auth.ApiSecret));
            var credentials = ExchangeCredentials.CreateFrom(exchangeName, apiCredentials);
            var environments = new Dictionary<string, string> { [exchangeName] = auth.Environment };

            var socketClient = clientProvider.GetSocketClient(accountId.ToString(), credentials, environments);
          
            var cts = new CancellationTokenSource();
            _subscriptions.TryAdd(accountId, new AccountUpdateSubscription(
                accountId,
                socketClient,
                new AccountCallbacks(connectionId, balanceHandler, orderHandler, tradeHandler, statusHandler),
                cts));

            var trackers = trackerFactory.CreateUserSpotDataTracker(
                accountId.ToString(),
                credentials,
                new SpotUserDataTrackerConfig(),
                environments,
                [exchangeName]);

            foreach (var tracker in trackers)
            {
                tracker.Balances.OnUpdate += x => HandleBalanceUpdate(accountId, x);
                tracker.Orders.OnUpdate   += x => HandleOrderUpdate(accountId, x);
                tracker.Trades!.OnUpdate  += x => HandleTradeUpdate(accountId, x);
                tracker.OnConnectedChange += (type, connected) => ProcessConnectedChange(accountId, tracker.Exchange, type, connected);
            }

            var tasks = trackers.Select(x => (x.Exchange, x.StartAsync())).ToList();
            await Task.WhenAll(tasks.Select(x => x.Item2));

            var failures = tasks.Where(x => !x.Item2.Result.Success).ToList();
            if (failures.Count > 0)
            {
                var errors = string.Join("; ", failures.Select(x => x.Item2.Result.Error?.Message ?? "Unknown"));
                logger.LogError("Failed to start subscription for account {AccountId} on {Exchange}: {Errors}", accountId, exchangeName, errors);
            }

            logger.LogDebug("User subscription for account {AccountId} connected", accountId);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task UnsubscribeAsync(Guid accountId, string connectionId)
    {
        if (!_userSemaphores.TryGetValue(accountId, out var semaphore))
            return;

        await semaphore.WaitAsync();
        try
        {
            if (!_subscriptions.TryGetValue(accountId, out var subscription))
                return;

            if (!subscription.ConnectionIds.Any(x => x == connectionId))
                return;

            subscription.Remove(connectionId);
            if (subscription.CallbackCount == 0)
            {
                logger.LogDebug("Unsubscribed account {AccountId}, no listeners — closing connection", accountId);
                await subscription.SocketClient.UnsubscribeAllAsync();
                _subscriptions.Remove(accountId, out _);
                await subscription.Cts.CancelAsync();
                subscription.Cts.Dispose();
            }
            else
            {
                logger.LogDebug("Unsubscribed account {AccountId}, {Count} listener(s) remaining", accountId, subscription.CallbackCount);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void ProcessConnectedChange(Guid accountId, string exchange, UserDataType type, bool connected)
    {
        var evnt = new SubscriptionEvent(connected ? StreamStatus.Restored : StreamStatus.Interrupted) { Exchange = exchange };

        if (_subscriptions.TryGetValue(accountId, out var subscription))
            subscription.Invoke(evnt);
    }

    private Task HandleBalanceUpdate(Guid accountId, UserDataUpdate<SharedBalance[]> update)
    {
        if (_subscriptions.TryGetValue(accountId, out var subscription))
            subscription.Invoke(update);

        return Task.CompletedTask;
    }

    private Task HandleOrderUpdate(Guid accountId, UserDataUpdate<SharedSpotOrder[]> update)
    {
        if (_subscriptions.TryGetValue(accountId, out var subscription))
            subscription.Invoke(update);

        return Task.CompletedTask;
    }

    private Task HandleTradeUpdate(Guid accountId, UserDataUpdate<SharedUserTrade[]> update)
    {
        if (_subscriptions.TryGetValue(accountId, out var subscription))
            subscription.Invoke(update);

        return Task.CompletedTask;
    }
}
