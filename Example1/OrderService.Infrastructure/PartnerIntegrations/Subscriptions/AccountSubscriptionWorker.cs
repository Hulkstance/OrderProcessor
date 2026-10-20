using CryptoExchange.Net.SharedApis;
using CryptoExchange.Net.Trackers.UserData.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Accounts;
using OrderService.Infrastructure.PartnerIntegrations.Models;

namespace OrderService.Infrastructure.PartnerIntegrations.Subscriptions;

internal sealed class AccountSubscriptionWorker : BackgroundService
{
    private const string ConnectionId = "server";

    private readonly ILogger<AccountSubscriptionWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserSubscriptionService _userSubscriptionService;
    private readonly AccountSubscriptionQueue _subscriptionQueue;
    private readonly IPublishOutput<OrderEvent> _orderOutput;
    private readonly IPublishOutput<BalanceEvent> _balanceOutput;
    private readonly IPublishOutput<TradeEvent> _tradeOutput;

    private readonly DataBatcher<BalanceEvent> _balanceBatcher;
    private readonly DataBatcher<OrderEvent> _orderBatcher;
    private readonly DataBatcher<TradeEvent> _userTradeBatcher;

    public AccountSubscriptionWorker(
        ILogger<AccountSubscriptionWorker> logger,
        IServiceScopeFactory scopeFactory,
        UserSubscriptionService userSubscriptionService,
        IPublishOutput<OrderEvent> orderOutput,
        IPublishOutput<BalanceEvent> balanceOutput,
        IPublishOutput<TradeEvent> tradeOutput,
        AccountSubscriptionQueue subscriptionQueue)
    {
        _logger               = logger;
        _scopeFactory         = scopeFactory;
        _userSubscriptionService = userSubscriptionService;
        _subscriptionQueue    = subscriptionQueue;
        _orderOutput          = orderOutput;
        _balanceOutput        = balanceOutput;
        _tradeOutput          = tradeOutput;

        _balanceBatcher = new DataBatcher<BalanceEvent>(
            TimeSpan.FromSeconds(1),
            PublishBalancesAsync);

        _orderBatcher = new DataBatcher<OrderEvent>(
            TimeSpan.FromSeconds(0.5),
            PublishOrdersAsync,
            (xOld, xUpdate) => xOld with
            {
                QuantityFilledBase  = xUpdate.QuantityFilledBase,
                QuantityFilledQuote = xUpdate.QuantityFilledQuote,
                Status              = xUpdate.Status,
                UpdateTime          = xUpdate.UpdateTime
            });

        _userTradeBatcher = new DataBatcher<TradeEvent>(
            TimeSpan.FromSeconds(0.5),
            PublishTradesAsync);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _balanceBatcher.StartAsync();
        await _orderBatcher.StartAsync();
        await _userTradeBatcher.StartAsync();
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _balanceBatcher.StopAsync();
        await _orderBatcher.StopAsync();
        await _userTradeBatcher.StopAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SeedQueueAsync(stoppingToken);

        await foreach (var account in _subscriptionQueue.ReadAllAsync(stoppingToken))
            await SubscribeAccount(account, stoppingToken);
    }

    private async Task SeedQueueAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var accounts = await dbContext.Accounts
            .Where(a => a.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Seeding subscription queue with {Count} active account(s)", accounts.Count);

        foreach (var account in accounts)
            _subscriptionQueue.Enqueue(account);
    }

    private Task SubscribeAccount(Account account, CancellationToken ct)
    {
        var auth = new UserExchangeAuthentication
        {
            Exchange       = account.Exchange,
            Environment    = account.Environment,
            ApiKey         = account.ApiKey,
            ApiSecret      = account.ApiSecret,
            Passphrase     = account.Passphrase
        };

        return _userSubscriptionService.SubscribeAsync(
            ConnectionId,
            account.Id,
            auth,
            balanceHandler: update => _ = ProcessBalanceUpdate(account.Id, update),
            orderHandler:   update => _ = ProcessOrderUpdate(account.Id, update),
            tradeHandler:   update => _ = ProcessUserTradeUpdate(account.Id, update),
            statusHandler:  evnt => _logger.LogInformation(
                "Account {AccountId} {Exchange} stream {Status}",
                account.Id, evnt.Exchange, evnt.Status),
            ct);
    }

    private async Task ProcessBalanceUpdate(Guid accountId, UserDataUpdate<SharedBalance[]> @event)
    {
        _logger.LogDebug("Received balance update for account {AccountId}, exchange {Exchange}", accountId, @event.Exchange);
        await _balanceBatcher.AddAsync(@event.Data.ToDictionary(
            x => $"{accountId}-{@event.Exchange}-{x.Asset}",
            x => new BalanceEvent(accountId, @event.Exchange, x.Asset, x.Available, x.Total)));
    }

    private async Task ProcessOrderUpdate(Guid accountId, UserDataUpdate<SharedSpotOrder[]> @event)
    {
        _logger.LogDebug("Received order update for account {AccountId}, exchange {Exchange}", accountId, @event.Exchange);

        var trades = @event.Data.Where(x => x.LastTrade != null).Select(y => y.LastTrade!);
        await _userTradeBatcher.AddAsync(trades.ToDictionary(
            x => $"{accountId}-{@event.Exchange}-{x.Id}",
            x => new TradeEvent(accountId, @event.Exchange, x.Symbol, x.OrderId, x.Id, x.Price, x.Quantity, x.Timestamp, x.Side, x.Fee, x.FeeAsset, x.Role)));

        await _orderBatcher.AddAsync(
            @event.Data
                .GroupBy(x => x.OrderId)
                .Select(x => x.OrderByDescending(y => y.QuantityFilled?.QuantityInBaseAsset ?? y.QuantityFilled?.QuantityInQuoteAsset).First())
                .ToDictionary(
                    x => $"{accountId}-{@event.Exchange}-{x.OrderId}",
                    x => new OrderEvent(
                        accountId,
                        @event.Exchange,
                        x.OrderId,
                        x.ClientOrderId ?? string.Empty,
                        x.Status,
                        x.QuantityFilled?.QuantityInBaseAsset,
                        x.QuantityFilled?.QuantityInQuoteAsset,
                        x.AveragePrice,
                        x.UpdateTime ?? DateTime.UtcNow)));
    }

    private async Task ProcessUserTradeUpdate(Guid accountId, UserDataUpdate<SharedUserTrade[]> @event)
    {
        _logger.LogDebug("Received user trade update for account {AccountId}, exchange {Exchange}", accountId, @event.Exchange);
        await _userTradeBatcher.AddAsync(@event.Data.ToDictionary(
            x => $"{accountId}-{@event.Exchange}-{x.Id}",
            x => new TradeEvent(accountId, @event.Exchange, x.Symbol, x.OrderId, x.Id, x.Price, x.Quantity, x.Timestamp, x.Side, x.Fee, x.FeeAsset, x.Role)));
    }

    private Task PublishBalancesAsync(Dictionary<string, BalanceEvent> dict)
        => _balanceOutput.PublishAsync(new PublishItem<BalanceEvent> { Data = dict.Values.ToList() });

    private Task PublishOrdersAsync(Dictionary<string, OrderEvent> dict)
        => _orderOutput.PublishAsync(new PublishItem<OrderEvent> { Data = dict.Values.ToList() });

    private Task PublishTradesAsync(Dictionary<string, TradeEvent> dict)
        => _tradeOutput.PublishAsync(new PublishItem<TradeEvent> { Data = dict.Values.ToList() });
}
