namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed record BalanceEvent(
    Guid    AccountId,
    string  Exchange,
    string  Asset,
    decimal Available,
    decimal Total);
