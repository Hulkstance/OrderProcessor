using OrderService.Domain.Common.Base;

namespace OrderService.Domain.Orders;

public class OrderFill : Entity<Guid>
{
    public const int ExchangeTradeIdMaxLength = 100;
    public const int FeeAssetMaxLength = 20;
    
    private OrderFill() { } // EF Core
    
    private OrderFill(
        Guid orderId,
        string externalId,
        decimal fillPrice,
        decimal fillQuantity,
        decimal? feeAmount,
        string? feeAsset,
        DateTime filledAt)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ExternalId = externalId;
        FillPrice = fillPrice;
        FillQuantity = fillQuantity;
        FeeAmount = feeAmount;
        FeeAsset = feeAsset;
        FilledAt = filledAt;
        IsActive = true;
    }
    
    public Guid OrderId { get; private set; }
    public string ExternalId
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ExternalId));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, ExchangeTradeIdMaxLength, nameof(ExternalId));
            field = value;
        }
    } = null!;
    
    public decimal FillPrice { get; private set; }
    public decimal FillQuantity { get; private set; }
    public decimal? FeeAmount { get; private set; }
    public string? FeeAsset { get; private set; }
    public DateTime FilledAt { get; private set; }
    
    public Order Order { get; private set; } = null!;
    
    public static OrderFill Create(
        Guid orderId,
        string exchangeTradeId,
        decimal fillPrice,
        decimal fillQuantity,
        DateTime filledAt,
        decimal? feeAmount = null,
        string? feeAsset = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fillPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fillQuantity);
        
        if (feeAmount.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(feeAmount.Value, nameof(feeAmount));
        
        return new OrderFill(
            orderId,
            exchangeTradeId,
            fillPrice,
            fillQuantity,
            feeAmount,
            feeAsset,
            filledAt);
    }
}
