using OrderService.Domain.Accounts;
using OrderService.Domain.Common.Base;

namespace OrderService.Domain.ExchangeLogs;

public class ExchangeLog : AggregateRoot<Guid>
{
    public const int MessageTypeMaxLength = 255;
    
    private ExchangeLog() { } // EF Core
    
    private ExchangeLog(
        Guid accountId,
        string? exchangeOrderId,
        string? clientOrderId,
        string messageType,
        string? requestPayload,
        string? responsePayload,
        string? errorText,
        bool isSuccess)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        ExchangeOrderId = exchangeOrderId;
        ClientOrderId = clientOrderId;
        MessageType = messageType;
        RequestPayload = requestPayload;
        ResponsePayload = responsePayload;
        ErrorText = errorText;
        IsSuccess = isSuccess;
        IsActive = true;
    }
    
    public Guid AccountId { get; private set; }
    public string? ExchangeOrderId { get; private set; }
    public string? ClientOrderId { get; private set; }
    
    public string MessageType
    {
        get;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(MessageType));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MessageTypeMaxLength, nameof(MessageType));
            field = value;
        }
    } = null!;
    
    public string? RequestPayload { get; private set; }
    public string? ResponsePayload { get; private set; }
    public string? ErrorText { get; private set; }
    public bool IsSuccess { get; private set; }

    public Account Account { get; private set; } = null!;
    
    public static ExchangeLog Create(
        Guid accountId,
        string? exchangeOrderId,
        string? clientOrderId,
        string messageType,
        bool isSuccess,
        string? requestPayload = null,
        string? responsePayload = null,
        string? errorText = null)
    {
        return new ExchangeLog(
            accountId,
            exchangeOrderId,
            clientOrderId,
            messageType,
            requestPayload,
            responsePayload,
            errorText,
            isSuccess);
    }
}
