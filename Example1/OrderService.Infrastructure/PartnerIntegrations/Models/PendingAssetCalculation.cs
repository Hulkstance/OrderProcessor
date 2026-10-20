namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed class PendingAssetCalculation
{
    public string Exchange { get; init; } = null!;
    public string Asset { get; init; } = null!;
}
