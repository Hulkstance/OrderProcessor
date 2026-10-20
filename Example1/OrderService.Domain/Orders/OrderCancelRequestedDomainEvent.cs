using OrderService.Domain.Common.Interfaces;

namespace OrderService.Domain.Orders;

public sealed record OrderCancelRequestedDomainEvent(Guid OrderId) : IDomainEvent;
