namespace OrderService.Application.Common.Interfaces;

public interface IExchangeLogRepository
{
    Task AddLogAsync(ExchangeLogRequest request, CancellationToken cancellationToken = default);
}

public class ExchangeLogRequest
{
    public Guid AccountId { get; init; }
    public string? ExchangeOrderId { get; init; }
    public string? ClientOrderId { get; init; }
    public string MessageType { get; init; } = null!;
    public bool IsSuccess { get; init; }

    public string? RequestPayload { get; init; }
    public string? ResponsePayload { get; init; }
    public string? ErrorText { get; init; }
}