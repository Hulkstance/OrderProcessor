using Microsoft.Extensions.Logging;
using OrderService.Domain.Orders;

namespace OrderService.Application.Orders.Services;

/// <summary>
/// Application-layer domain service that owns all logic for applying exchange
/// status events to an Order aggregate.
/// </summary>
public class OrderDomainService(ILogger<OrderDomainService> logger)
{
    /// <summary>
    /// Exchange acknowledged the order is live on the book. Binance: NEW / "Open".
    /// </summary>
    public void MarkOrderWorking(Order order, string exchangeOrderId)
    {
        if (!order.CanBeAccepted())
        {
            logger.LogDebug("Order {OrderId} in state {State} cannot be marked Working — skipping", order.Id, order.State.Value);
            return;
        }

        order.Accept(exchangeOrderId);
    }

    /// <summary>
    /// Exchange reported a partial fill — Open with ExecutedQuantity > 0.
    /// Fills are recorded separately via the trade channel, not here.
    /// </summary>
    public void MarkOrderPartiallyFilled(
        Order order,
        string exchangeOrderId,
        decimal executedQuantity,
        decimal? averagePrice)
    {
        if (!order.CanBeFilled())
        {
            logger.LogDebug("Order {OrderId} in terminal state {State} — skipping PartiallyFilled event", order.Id, order.State.Value);
            return;
        }

        order.PartialFill(exchangeOrderId, executedQuantity, averagePrice);
    }

    /// <summary>
    /// Order fully filled. Binance: FILLED.
    /// Fills are recorded separately via the trade channel, not here.
    /// </summary>
    public void MarkOrderFilled(
        Order order,
        string exchangeOrderId,
        decimal executedQuantity,
        decimal? averagePrice)
    {
        if (!order.CanBeFilled())
        {
            logger.LogDebug("Order {OrderId} in terminal state {State} — skipping Filled event", order.Id, order.State.Value);
            return;
        }

        order.Fill(exchangeOrderId, executedQuantity, averagePrice);
    }

    /// <summary>
    /// Exchange confirmed cancellation. Binance: CANCELED.
    /// </summary>
    public void MarkOrderCancelled(Order order)
    {
        if (!order.CanBeCancelled())
        {
            logger.LogDebug("Order {OrderId} in state {State} cannot be cancelled — skipping Canceled event", order.Id, order.State.Value);
            return;
        }

        order.Cancel();
    }

    /// <summary>
    /// Order expired (GTD time passed, or IOC/FOK could not be filled). Binance: EXPIRED.
    /// </summary>
    public void MarkOrderExpired(Order order) => order.Expire();

    /// <summary>
    /// Exchange rejected the order (invalid parameters, insufficient balance, etc.).
    /// </summary>
    public void MarkOrderRejected(Order order) => order.Reject();
}
