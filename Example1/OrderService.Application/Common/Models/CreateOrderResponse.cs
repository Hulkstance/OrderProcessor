namespace OrderService.Application.Common.Models;

public class CreateOrderResponse
{
    public required string ExchangeOrderId { get; init; }
    public required string ClientOrderId { get; init; }
    public string? Status { get; init; }
}
