using CryptoClients.Net.Interfaces;
using CryptoClients.Net.Models;
using CryptoExchange.Net.SharedApis;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.PartnerIntegrations;

internal sealed class ExchangeFeeService(
    IAppDbContext dbContext,
    IExchangeUserClientProvider clientProvider) : IExchangeFeeService
{
    public async Task<ErrorOr<ExchangeFeesDto>> GetFeesAsync(Guid accountId, string exchange, string symbolId, CancellationToken ct)
    {
        var account = await dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == accountId, ct);

        if (account is null)
            return Error.NotFound("Account.NotFound", "Account not found");

        var apiCredentials = ExchangeCredentials.CreateCredentialsForExchange(
            exchange,
            new DynamicCredentials(TradingMode.Spot, account.ApiKey, account.ApiSecret));
        var credentials = ExchangeCredentials.CreateFrom(exchange, apiCredentials);
        var environments = new Dictionary<string, string?> { [exchange] = account.Environment };

        var client = clientProvider.GetRestClient(accountId.ToString(), credentials, environments);

        var feeClient = client.GetFeeClient(TradingMode.Spot, exchange);
        if (feeClient is null)
            return Error.Failure("Fees.NotAvailable", "Fee information is not available for this exchange");

        var symbolParts = symbolId.Split('-');
        var fees = await feeClient.GetFeesAsync(new GetFeeRequest(new SharedSymbol(TradingMode.Spot, symbolParts[1], symbolParts[2])), ct);
        if (!fees)
            return Error.Failure("Fees.RequestFailed", fees.Error?.Message ?? "Failed to retrieve fees");

        return new ExchangeFeesDto(exchange, fees.Data.MakerFee, fees.Data.TakerFee);
    }
}
