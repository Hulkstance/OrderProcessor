using OrderService.Domain.Accounts;
using OrderService.Domain.Common.Base;

namespace OrderService.Domain.Orders;

public class Order : AggregateRoot<Guid>
{
    public const int ExchangeMaxLength = 50;
    public const int SymbolIdMaxLength = 100;
    public const int ExternalIdMaxLength = 100;
    public const int InternalIdMaxLength = 64;

    private readonly List<OrderFill> _fills = [];

    private Order() { } // EF Core

    private Order(
        Guid accountId,
        string exchange,
        string symbolId,
        OrderSide side,
        OrderType orderType,
        string internalId,
        decimal quantity,
        decimal? price,
        TimeInForce? timeInForce)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Exchange = exchange;
        SymbolId = symbolId;
        Side = side;
        OrderType = orderType;
        InternalId = internalId;
        Quantity = quantity;
        Price = price;
        TimeInForce = timeInForce;
        State = new OrderState(OrderStatus.Created);
        IsActive = true;
    }

    public Guid AccountId { get; private set; }
    public string Exchange { get; private set; } = null!;
    public string SymbolId { get; private set; } = null!;
    public OrderSide Side { get; private set; }
    public OrderType OrderType { get; private set; }
    public TimeInForce? TimeInForce { get; private set; }
    
    public decimal Quantity
    {
        get;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    }
    
    public decimal? Price
    {
        get;
        private set
        {
            if (value.HasValue)
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.Value);

            field = value;
        }
    }
    
    public OrderState State { get; private set; } = null!;
    
    public decimal ExecutedQuantity
    {
        get;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    }
    
    public decimal? AveragePrice
    {
        get;
        private set
        {
            if (value.HasValue)
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.Value);

            field = value;
        }
    }

    public string? ExternalId
    {
        get;
        private set
        {
            if (!string.IsNullOrEmpty(value))
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, ExternalIdMaxLength, nameof(ExternalId));

            field = value;
        }
    }
    
    public string InternalId
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(InternalId));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, InternalIdMaxLength, nameof(InternalId));
            field = value;
        }
    } = null!;

    public Account Account { get; private set; } = null!;
    public IReadOnlyList<OrderFill> Fills => _fills.AsReadOnly();

    public static Order Create(
        Guid accountId,
        string exchange,
        string symbolId,
        OrderSide side,
        OrderType orderType,
        string internalId,
        decimal quantity,
        decimal? price = null,
        TimeInForce? timeInForce = null)
    {
        if (orderType == OrderType.Limit)
        {
            if (price is null)
                throw new ArgumentException("Limit orders must have a price.", nameof(price));

            if (timeInForce is null)
                throw new ArgumentException("Limit orders must have a TimeInForce.", nameof(timeInForce));
        }

        if (orderType == OrderType.Market)
        {
            if (price is not null)
                throw new ArgumentException("Market orders must not have a price.", nameof(price));

            if (timeInForce is not null)
                throw new ArgumentException("Market orders must not have a TimeInForce.", nameof(timeInForce));
        }

        var order = new Order(
            accountId,
            exchange,
            symbolId,
            side,
            orderType,
            internalId,
            quantity,
            price,
            timeInForce);

        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id));

        return order;
    }

    // -------------------------------------------------------------------------
    // Internal lifecycle
    // -------------------------------------------------------------------------

    public void StartSubmitting() => State = State.StartSubmitting();
    public void ConfirmSubmitted(string externalId)
    {
        State = State.ConfirmSubmitted();
        ExternalId = externalId;
    }

    // -------------------------------------------------------------------------
    // Exchange-driven updates (called from OrderUpdateProcessor)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Exchange acknowledged the order is live. Binance: NEW.
    /// </summary>
    public void Accept(string externalId)
    {
        if (string.IsNullOrWhiteSpace(ExternalId))
            ExternalId = externalId;

        State = State.Accept();
    }

    /// <summary>
    /// Exchange reported a partial fill. Updates cumulative executed quantity and
    /// average price. Binance sends one event per fill, each carrying the running totals.
    /// </summary>
    public void PartialFill(string externalId, decimal executedQuantity, decimal? averagePrice)
    {
        if (string.IsNullOrWhiteSpace(ExternalId))
            ExternalId = externalId;

        State = State.PartialFill();
        ExecutedQuantity = executedQuantity;

        if (averagePrice.HasValue)
            AveragePrice = averagePrice;
    }

    /// <summary>
    /// Order fully filled. Binance: FILLED.
    /// </summary>
    public void Fill(string externalId, decimal executedQuantity, decimal? averagePrice)
    {
        if (string.IsNullOrWhiteSpace(ExternalId))
            ExternalId = externalId;

        State = State.Fill();
        ExecutedQuantity = executedQuantity;

        if (averagePrice.HasValue)
            AveragePrice = averagePrice;
    }

    /// <summary>
    /// We submitted a cancel request to the exchange.
    /// </summary>
    public void RequestCancel() => State = State.RequestCancel();

    /// <summary>
    /// Exchange confirmed the cancel. Also called when the exchange cancels without
    /// our request — e.g. IOC/FOK orders the exchange reports as CANCELED.
    /// </summary>
    public void Cancel() => State = State.Cancel();

    /// <summary>
    /// Order expired. Binance sends EXPIRED for GTD orders past their time, and
    /// for IOC/FOK orders that could not be filled.
    /// </summary>
    public void Expire() => State = State.Expire();

    /// <summary>
    /// Exchange rejected the order — invalid parameters, insufficient balance, etc.
    /// </summary>
    public void Reject() => State = State.Reject();

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    public bool IsOnStatus(OrderStatus status) => State.Is(status);

    public bool IsOnAnyStatus(params OrderStatus[] statuses)
    {
        foreach (var status in statuses)
            if (State.Is(status)) return true;

        return false;
    }

    public bool IsTerminalState() => State.IsTerminal();

    public bool CanBeAccepted() =>
        IsOnAnyStatus(OrderStatus.Created, OrderStatus.Submitting, OrderStatus.Submitted);

    public bool CanBeFilled() => !IsTerminalState();

    public bool CanBeCancelled() =>
        IsOnAnyStatus(OrderStatus.CancelRequested, OrderStatus.Accepted, OrderStatus.PartiallyFilled);

    public void AddFill(OrderFill fill) => _fills.Add(fill);

    public decimal GetFilledQuantity() => _fills.Sum(x => x.FillQuantity);

    public decimal GetRemainingQuantity() => Quantity - GetFilledQuantity();
}
