namespace OrderService.Domain.Orders;

public enum OrderStatus
{
    Created,
    Submitting,
    Submitted,
    Accepted,
    PartiallyFilled,
    Filled,
    CancelRequested,
    Cancelled,
    Rejected,
    Expired,
    Unknown
}
