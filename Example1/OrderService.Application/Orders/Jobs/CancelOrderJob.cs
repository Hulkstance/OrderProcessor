using BuildingBlocks.Jobs;
using Hangfire;
using Hangfire.Server;
using Hangfire.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Common.Models;

namespace OrderService.Application.Orders.Jobs;

public sealed record CancelOrderJobOptions(Guid OrderId) : IJobOptions;

[AutomaticRetry(Attempts = 5)]
[Queue("critical")]
public class CancelOrderJob(
    IAppDbContext dbContext,
    ILogger<CancelOrderJob> logger,
    IServiceProvider serviceProvider)
    : IJob<CancelOrderJobOptions>
{
    public async Task Perform(CancelOrderJobOptions jobOptions, PerformContext? performContext)
    {
        var orderId = jobOptions.OrderId;
        
        performContext?.AddTags($"OrderId:{orderId}");

        var order = await dbContext.Orders
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null)
        {
            logger.LogError("Order {OrderId} not found", orderId);

            throw new InvalidOperationException($"Order {orderId} not found");
        }

        // If order doesn't have an ExchangeOrderId yet, it hasn't been submitted to the exchange
        // so there's nothing to cancel on the exchange side
        if (string.IsNullOrWhiteSpace(order.ExternalId))
        {
            logger.LogWarning(
                "Order {OrderId} has no ExchangeOrderId - marking as cancelled without exchange call",
                order.Id);

            performContext?.AddTags("NoExchangeOrderId", "Cancelled");
            return;
        }

        var exchangeName = order.Account.Exchange;
        logger.LogInformation(
            "Cancelling order {OrderId} (ExchangeOrderId: {ExchangeOrderId}) on {Exchange}",
            order.Id,
            order.ExternalId,
            exchangeName);

        performContext?.AddTags($"Exchange:{exchangeName}", $"ExchangeOrderId:{order.ExternalId}");

        var partnerIntegration = GetPartnerIntegration(exchangeName);

        if (partnerIntegration == null)
        {
            logger.LogError("No partner integration found for exchange {Exchange}", exchangeName);

            performContext?.AddTags("NoIntegration", "Failed");

            throw new InvalidOperationException($"No partner integration found for exchange {exchangeName}");
        }

        var symbolParts = order.SymbolId.Split('-');
        var request = new CancelOrderRequest
        {
            Account = order.Account,
            BaseAsset = symbolParts[1],
            QuoteAsset = symbolParts[2],
            ClientOrderId = order.InternalId
        };

        var response = await partnerIntegration.CancelOrderAsync(request, CancellationToken.None);

        if (response.Success)
        {
            logger.LogInformation("Order {OrderId} successfully cancelled on {Exchange}", order.Id, exchangeName);

            performContext?.AddTags("Success", "Cancelled");
        }
        else
        {
            logger.LogError(
                "Failed to cancel order {OrderId} on {Exchange}: {ErrorCode} - {ErrorMessage}",
                order.Id,
                exchangeName,
                response.Error.Code,
                response.Error.Message);

            performContext?.AddTags($"Error:{response.Error.Code}", "Failed");

            throw new InvalidOperationException($"Failed to cancel order: {response.Error.Code} - {response.Error.Message}");
        }

        await dbContext.SaveChangesAsync();
    }

    private IPartnerIntegration? GetPartnerIntegration(string exchangeCode)
    {
        var integrations = serviceProvider.GetServices<IPartnerIntegration>();
        return integrations.FirstOrDefault(x => x.Name.Equals(exchangeCode, StringComparison.OrdinalIgnoreCase));
    }
}
