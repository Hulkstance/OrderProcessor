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

public sealed record CreateOrderJobOptions(Guid OrderId) : IJobOptions;

[AutomaticRetry(Attempts = 5)]
[Queue("critical")]
public class CreateOrderJob(
    IAppDbContext dbContext,
    ILogger<CreateOrderJob> logger,
    IServiceProvider serviceProvider)
    : IJob<CreateOrderJobOptions>
{
    public async Task Perform(CreateOrderJobOptions jobOptions, PerformContext? performContext)
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

        order.StartSubmitting();
        await dbContext.SaveChangesAsync();

        var exchangeName = order.Account.Exchange;
        logger.LogInformation("Submitting order {OrderId} to {Exchange}", order.Id, exchangeName);

        performContext?.AddTags($"Exchange:{exchangeName}");

        var partnerIntegration = GetPartnerIntegration(exchangeName);

        if (partnerIntegration == null)
        {
            logger.LogError("No partner integration found for exchange {Exchange}", exchangeName);
            throw new InvalidOperationException($"No integration available for exchange {exchangeName}");
        }

        var symbolParts = order.SymbolId.Split('-');
        var request = new CreateOrderRequest
        {
            Account = order.Account,
            BaseAsset = symbolParts[1],
            QuoteAsset = symbolParts[2],
            Side = order.Side,
            OrderType = order.OrderType,
            Quantity = order.Quantity,
            Price = order.Price,
            TimeInForce = order.TimeInForce,
            ClientOrderId = order.InternalId
        };

        var response = await partnerIntegration.CreateOrderAsync(request, CancellationToken.None);

        if (response.Success)
        {
            order.ConfirmSubmitted(response.Data.ExchangeOrderId);

            logger.LogInformation(
                "Order {OrderId} successfully submitted to {Exchange} with ExchangeOrderId {ExchangeOrderId}",
                order.Id,
                exchangeName,
                response.Data.ExchangeOrderId);

            performContext?.AddTags($"ExchangeOrderId:{response.Data.ExchangeOrderId}", "Success");
        }
        else
        {
            logger.LogError(
                "Failed to submit order {OrderId} to {Exchange}: {ErrorCode} - {ErrorMessage}",
                order.Id,
                exchangeName,
                response.Error.Code,
                response.Error.Message);

            performContext?.AddTags($"Error:{response.Error.Code}", "Failed");

            throw new InvalidOperationException($"Failed to submit order: {response.Error.Code} - {response.Error.Message}");
        }

        await dbContext.SaveChangesAsync();
    }

    private IPartnerIntegration? GetPartnerIntegration(string exchangeCode)
    {
        var integrations = serviceProvider.GetServices<IPartnerIntegration>();
        return integrations.FirstOrDefault(x => x.Name.Equals(exchangeCode, StringComparison.OrdinalIgnoreCase));
    }
}
