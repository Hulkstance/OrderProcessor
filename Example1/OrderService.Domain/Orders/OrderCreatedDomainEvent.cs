using OrderService.Domain.Common.Interfaces;

namespace OrderService.Domain.Orders;

public sealed record OrderCreatedDomainEvent(Guid OrderId) : IDomainEvent;
