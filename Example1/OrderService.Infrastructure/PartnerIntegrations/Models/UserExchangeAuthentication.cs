namespace OrderService.Infrastructure.PartnerIntegrations.Models;

internal sealed class UserExchangeAuthentication
{
    public string Exchange { get; set; } = null!;
    public string Environment { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string ApiSecret { get; set; } = null!;
    public string? Passphrase { get; set; }
}
