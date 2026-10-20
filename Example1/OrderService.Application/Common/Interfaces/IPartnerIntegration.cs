using OrderService.Application.Common.Models;

namespace OrderService.Application.Common.Interfaces;

public interface IPartnerIntegration
{
    string Name { get; }
 
    Task<ExchangeResponse<CreateOrderResponse>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);
    
    Task<ExchangeResponse<OrderInfo>> GetOrderAsync(
        GetOrderRequest request,
        CancellationToken cancellationToken = default);
    
    Task<ExchangeResponse<IEnumerable<OrderInfo>>> GetActiveOrdersAsync(
        GetOrdersRequest request,
        CancellationToken cancellationToken = default);
    
    Task<ExchangeResponse<IEnumerable<OrderInfo>>> GetOrderHistoryAsync(
        GetOrdersRequest request,
        CancellationToken cancellationToken = default);
    
    Task<ExchangeResponse<OrderInfo>> CancelOrderAsync(
        CancelOrderRequest request,
        CancellationToken cancellationToken = default);
}
