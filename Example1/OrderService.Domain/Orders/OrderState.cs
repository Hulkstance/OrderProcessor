using OrderService.Domain.Common.Interfaces;

namespace OrderService.Domain.Orders;

/// <summary>
/// Value object that owns the order state machine.
/// Each transition method enforces which source states are valid and returns a new
/// instance — the aggregate never mutates Status directly.
/// </summary>
public sealed class OrderState : IValueObject
{
    private OrderState() { } // EF Core
    public OrderState(OrderStatus status) => Value = status;

    public OrderStatus Value { get; private set; }

    // -------------------------------------------------------------------------
    // Internal lifecycle (our system → exchange)
    // -------------------------------------------------------------------------

    /// <summary>Created → Submitting: job picked up the order and is calling the exchange.</summary>
    public OrderState StartSubmitting()
    {
        if (Value != OrderStatus.Created)
            throw new InvalidOperationException($"Cannot start submitting from {Value}.");

        return new(OrderStatus.Submitting);
    }

    /// <summary>Submitting → Submitted: exchange REST call returned an order ID.</summary>
    public OrderState ConfirmSubmitted()
    {
        if (Value != OrderStatus.Submitting)
            throw new InvalidOperationException($"Cannot confirm submission from {Value}.");

        return new(OrderStatus.Submitted);
    }

    /// <summary>
    /// → Accepted: exchange WebSocket confirmed the order is live (Binance: NEW).
    /// Allows from Created, Submitting, and Submitted because the WebSocket event
    /// can arrive before the Hangfire job has advanced the internal state.
    /// </summary>
    public OrderState Accept()
    {
        if (Value != OrderStatus.Created &&
            Value != OrderStatus.Submitting &&
            Value != OrderStatus.Submitted)
        {
            throw new InvalidOperationException($"Cannot accept from {Value}.");
        }

        return new(OrderStatus.Accepted);
    }

    // -------------------------------------------------------------------------
    // Fill transitions (driven by exchange WebSocket)
    // -------------------------------------------------------------------------

    /// <summary>
    /// → PartiallyFilled.
    /// Binance sends PARTIALLY_FILLED for each individual fill event.
    /// Early internal states are included because a fill can arrive before our
    /// Hangfire job has advanced the order to Accepted.
    /// </summary>
    public OrderState PartialFill()
    {
        if (Value != OrderStatus.Created &&
            Value != OrderStatus.Submitting &&
            Value != OrderStatus.Submitted &&
            Value != OrderStatus.Accepted &&
            Value != OrderStatus.PartiallyFilled)
        {
            throw new InvalidOperationException($"Cannot partially fill from {Value}.");
        }

        return new(OrderStatus.PartiallyFilled);
    }

    /// <summary>
    /// → Filled.
    /// CancelRequested is included because Binance can fill an order in the same
    /// moment a cancel request arrives — the fill wins.
    /// Early internal states are included for the same race condition as PartialFill.
    /// </summary>
    public OrderState Fill()
    {
        if (Value != OrderStatus.Created &&
            Value != OrderStatus.Submitting &&
            Value != OrderStatus.Submitted &&
            Value != OrderStatus.Accepted &&
            Value != OrderStatus.PartiallyFilled &&
            Value != OrderStatus.CancelRequested)
        {
            throw new InvalidOperationException($"Cannot fill from {Value}.");
        }

        return new(OrderStatus.Filled);
    }

    // -------------------------------------------------------------------------
    // Cancel transitions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Accepted | PartiallyFilled | Submitted → CancelRequested: we sent a cancel to the exchange.
    /// Submitted is included because the cancel might race with acknowledgement.
    /// </summary>
    public OrderState RequestCancel()
    {
        if (Value != OrderStatus.Accepted &&
            Value != OrderStatus.PartiallyFilled &&
            Value != OrderStatus.Submitted)
        {
            throw new InvalidOperationException($"Cannot request cancel from {Value}.");
        }

        return new(OrderStatus.CancelRequested);
    }

    /// <summary>
    /// CancelRequested | Accepted | PartiallyFilled → Cancelled.
    /// The exchange confirms the cancel. Accepted and PartiallyFilled are included
    /// for cases where the exchange cancels without us explicitly requesting it
    /// (e.g. IOC/FOK expiry reported as CANCELED rather than EXPIRED).
    /// </summary>
    public OrderState Cancel()
    {
        if (Value != OrderStatus.CancelRequested &&
            Value != OrderStatus.Accepted &&
            Value != OrderStatus.PartiallyFilled)
        {
            throw new InvalidOperationException($"Cannot cancel from {Value}.");
        }

        return new(OrderStatus.Cancelled);
    }

    // -------------------------------------------------------------------------
    // Terminal states from exchange
    // -------------------------------------------------------------------------

    /// <summary>
    /// Accepted | PartiallyFilled | CancelRequested → Expired.
    /// Binance sends EXPIRED for GTD orders whose time has passed, or for
    /// IOC/FOK orders that could not be fully filled.
    /// </summary>
    public OrderState Expire()
    {
        if (Value != OrderStatus.Accepted &&
            Value != OrderStatus.PartiallyFilled &&
            Value != OrderStatus.CancelRequested)
        {
            throw new InvalidOperationException($"Cannot expire from {Value}.");
        }

        return new(OrderStatus.Expired);
    }

    /// <summary>
    /// Any non-terminal state → Rejected.
    /// Binance can reject at submission time (invalid params) or, rarely, mid-life.
    /// </summary>
    public OrderState Reject()
    {
        if (IsTerminal())
            throw new InvalidOperationException($"Cannot reject an already terminal order ({Value}).");

        return new(OrderStatus.Rejected);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public bool IsTerminal() =>
        Value is OrderStatus.Filled
               or OrderStatus.Cancelled
               or OrderStatus.Rejected
               or OrderStatus.Expired;

    public bool Is(OrderStatus status) => Value == status;

    public override string ToString() => Value.ToString();
}
