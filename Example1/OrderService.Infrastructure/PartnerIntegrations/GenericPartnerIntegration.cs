using CryptoClients.Net.Interfaces;
using CryptoClients.Net.Models;
using CryptoExchange.Net.SharedApis;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Common.Models;
using OrderService.Domain.Orders;
using CancelOrderRequest = OrderService.Application.Common.Models.CancelOrderRequest;
using CreateOrderRequest = OrderService.Application.Common.Models.CreateOrderRequest;
using GetOrderRequest = OrderService.Application.Common.Models.GetOrderRequest;
using GetOrdersRequest = OrderService.Application.Common.Models.GetOrdersRequest;

namespace OrderService.Infrastructure.PartnerIntegrations;

public class GenericPartnerIntegration(
    string exchangeName,
    IExchangeUserClientProvider clientProvider,
    IExchangeLogRepository exchangeLogRepository) : IPartnerIntegration
{
    public string Name => exchangeName;

    public async Task<ExchangeResponse<CreateOrderResponse>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = GetRestClient(request.Account);
        var exchange = request.Account.Exchange;
        var symbol = new SharedSymbol(TradingMode.Spot, request.BaseAsset, request.QuoteAsset);

        var result = await client.PlaceSpotOrderAsync(exchange,
            new PlaceSpotOrderRequest(
                symbol,
                MapOrderSide(request.Side),
                MapOrderType(request.OrderType),
                SharedQuantity.Base(request.Quantity),
                request.Price,
                MapTimeInForce(request.TimeInForce))
            {
                ClientOrderId = request.ClientOrderId
            }, cancellationToken);

        await exchangeLogRepository.AddLogAsync(new ExchangeLogRequest
        {
            AccountId = request.Account.Id,
            ExchangeOrderId = result.Success ? result.Data.Id : null,
            ClientOrderId = request.ClientOrderId,
            MessageType = nameof(CreateOrderRequest),
            IsSuccess = result.Success,
            RequestPayload = result.RequestBody,
            ResponsePayload = result.OriginalData,
            ErrorText = result.Success ? null : $"{result.Error?.Code}: {result.Error?.Message}"
        }, cancellationToken);

        if (!result.Success)
            return ExchangeResponse<CreateOrderResponse>.CreateError(ParseError(result), result.ResponseStatusCode);

        return ExchangeResponse<CreateOrderResponse>.CreateSuccess(
            new CreateOrderResponse
            {
                ExchangeOrderId = result.Data.Id,
                ClientOrderId = request.ClientOrderId,
                Status = nameof(SharedOrderStatus.Open)
            },
            result.ResponseStatusCode);
    }

    public async Task<ExchangeResponse<OrderInfo>> GetOrderAsync(
        GetOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = GetRestClient(request.Account);
        var exchange = request.Account.Exchange;
        var symbol = new SharedSymbol(TradingMode.Spot, request.BaseAsset, request.QuoteAsset);

        var result = await client.GetSpotOrderByClientOrderIdAsync(exchange,
            new CryptoExchange.Net.SharedApis.GetOrderRequest(symbol, request.ClientOrderId),
            cancellationToken);

        if (!result.Success)
            return ExchangeResponse<OrderInfo>.CreateError(ParseError(result), result.ResponseStatusCode);

        return ExchangeResponse<OrderInfo>.CreateSuccess(MapToOrderInfo(result.Data), result.ResponseStatusCode);
    }

    public async Task<ExchangeResponse<IEnumerable<OrderInfo>>> GetActiveOrdersAsync(
        GetOrdersRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = GetRestClient(request.Account);
        var exchange = request.Account.Exchange;
        var symbol = request.BaseAsset != null && request.QuoteAsset != null
            ? new SharedSymbol(TradingMode.Spot, request.BaseAsset, request.QuoteAsset)
            : null;

        var result = await client.GetSpotOpenOrdersAsync(exchange,
            new GetOpenOrdersRequest(symbol!),
            cancellationToken);

        if (!result.Success)
            return ExchangeResponse<IEnumerable<OrderInfo>>.CreateError(ParseError(result), result.ResponseStatusCode);

        return ExchangeResponse<IEnumerable<OrderInfo>>.CreateSuccess(
            result.Data.Select(MapToOrderInfo),
            result.ResponseStatusCode);
    }

    public async Task<ExchangeResponse<IEnumerable<OrderInfo>>> GetOrderHistoryAsync(
        GetOrdersRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = GetRestClient(request.Account);
        var exchange = request.Account.Exchange;
        var symbol = new SharedSymbol(TradingMode.Spot, request.BaseAsset!, request.QuoteAsset!);

        var result = await client.GetSpotClosedOrdersAsync(exchange,
            new GetClosedOrdersRequest(symbol, request.StartTime, request.EndTime, request.Limit),
            ct: cancellationToken);

        if (!result.Success)
            return ExchangeResponse<IEnumerable<OrderInfo>>.CreateError(ParseError(result), result.ResponseStatusCode);

        return ExchangeResponse<IEnumerable<OrderInfo>>.CreateSuccess(
            result.Data.Select(MapToOrderInfo),
            result.ResponseStatusCode);
    }

    public async Task<ExchangeResponse<OrderInfo>> CancelOrderAsync(
        CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = GetRestClient(request.Account);
        var exchange = request.Account.Exchange;
        var symbol = new SharedSymbol(TradingMode.Spot, request.BaseAsset, request.QuoteAsset);

        var result = await client.CancelSpotOrderByClientOrderIdAsync(exchange,
            new CryptoExchange.Net.SharedApis.CancelOrderRequest(symbol, request.ClientOrderId),
            cancellationToken);

        await exchangeLogRepository.AddLogAsync(new ExchangeLogRequest
        {
            AccountId = request.Account.Id,
            ExchangeOrderId = result.Success ? result.Data.Id : null,
            ClientOrderId = request.ClientOrderId,
            MessageType = nameof(CancelOrderRequest),
            IsSuccess = result.Success,
            RequestPayload = result.RequestBody,
            ResponsePayload = result.OriginalData,
            ErrorText = result.Success ? null : $"{result.Error?.Code}: {result.Error?.Message}"
        }, cancellationToken);

        if (!result.Success)
            return ExchangeResponse<OrderInfo>.CreateError(ParseError(result), result.ResponseStatusCode);

        return ExchangeResponse<OrderInfo>.CreateSuccess(
            new OrderInfo
            {
                ExchangeOrderId = result.Data.Id,
                ClientOrderId = request.ClientOrderId,
                Symbol = string.Empty,
                Side = string.Empty,
                Type = string.Empty,
                Status = nameof(SharedOrderStatus.Canceled),
                Quantity = 0
            },
            result.ResponseStatusCode);
    }

    // -------------------------------------------------------------------------

    private IExchangeRestClient GetRestClient(Domain.Accounts.Account account)
    {
        var exchange = account.Exchange;
        var environment = account.Environment;

        var apiCredentials = ExchangeCredentials.CreateCredentialsForExchange(
            exchange,
            new DynamicCredentials(TradingMode.Spot, account.ApiKey, account.ApiSecret));
        var credentials = ExchangeCredentials.CreateFrom(exchange, apiCredentials);

        var environments = new Dictionary<string, string?> { [exchange] = environment };

        return clientProvider.GetRestClient(account.Id.ToString(), credentials, environments);
    }

    private static ExchangeError ParseError<T>(ExchangeWebResult<T> result) => new()
    {
        Code = result.Error?.Code?.ToString() ?? ExchangeErrorConstants.UnknownErrorCode,
        Message = result.Error?.Message ?? ExchangeErrorConstants.UnknownErrorMessage
    };

    private static OrderInfo MapToOrderInfo(SharedSpotOrder o) => new()
    {
        ExchangeOrderId = o.OrderId,
        ClientOrderId = o.ClientOrderId ?? string.Empty,
        Symbol = o.Symbol,
        Side = o.Side.ToString(),
        Type = o.OrderType.ToString(),
        Status = o.Status.ToString(),
        Quantity = o.OrderQuantity?.QuantityInBaseAsset ?? 0,
        Price = o.OrderPrice,
        ExecutedQuantity = o.QuantityFilled?.QuantityInBaseAsset ?? 0,
        AveragePrice = o.AveragePrice,
        CreateTime = o.CreateTime,
        UpdateTime = o.UpdateTime
    };

    private static SharedOrderSide MapOrderSide(OrderSide side) => side switch
    {
        OrderSide.Buy  => SharedOrderSide.Buy,
        OrderSide.Sell => SharedOrderSide.Sell,
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
    };

    private static SharedOrderType MapOrderType(OrderType type) => type switch
    {
        OrderType.Market => SharedOrderType.Market,
        OrderType.Limit  => SharedOrderType.Limit,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static SharedTimeInForce? MapTimeInForce(TimeInForce? tif) => tif switch
    {
        null                  => null,
        TimeInForce.GTC       => SharedTimeInForce.GoodTillCanceled,
        TimeInForce.IOC       => SharedTimeInForce.ImmediateOrCancel,
        TimeInForce.FOK       => SharedTimeInForce.FillOrKill,
        _ => throw new ArgumentOutOfRangeException(nameof(tif), tif, null)
    };
}
