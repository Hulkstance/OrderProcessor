namespace OrderService.Application.Common.Models;

public class OrderInfo
{
    public required string ExchangeOrderId { get; init; }
    public required string ClientOrderId { get; init; }
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? Price { get; init; }
    public decimal ExecutedQuantity { get; init; }
    public decimal? AveragePrice { get; init; }
    public DateTime? CreateTime { get; init; }
    public DateTime? UpdateTime { get; init; }
}
