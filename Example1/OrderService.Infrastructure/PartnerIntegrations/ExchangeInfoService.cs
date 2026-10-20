using CryptoClients.Net;
using Microsoft.Extensions.Configuration;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.PartnerIntegrations;

internal sealed class ExchangeInfoService(IConfiguration configuration) : IExchangeInfoService
{
    private readonly string[]? _enabledExchanges = configuration.GetSection("EnabledExchanges").Get<string[]>();

    public string[] GetAllNames()
        => _enabledExchanges ?? Exchanges.All.Select(x => x.Name).ToArray();

    public ExchangeStaticInfo? GetInfo(string exchange)
    {
        var info = Exchanges.All.FirstOrDefault(x => x.Name == exchange);
        if (info is null)
            return null;

        return new ExchangeStaticInfo(
            info.ApiEnvironments,
            info.ImageUrl,
            info.Url,
            info.Type.ToString());
    }
}
