using OrderService.Application.Common.Interfaces;
using OrderService.Domain.ExchangeLogs;

namespace OrderService.Infrastructure.Repositories;

public class ExchangeLogRepository(IAppDbContext dbContext) : IExchangeLogRepository
{
    public async Task AddLogAsync(ExchangeLogRequest request, CancellationToken cancellationToken = default)
    {
        var log = ExchangeLog.Create(
            request.AccountId,
            request.ExchangeOrderId,
            request.ClientOrderId,
            request.MessageType,
            request.IsSuccess,
            request.RequestPayload,
            request.ResponsePayload,
            request.ErrorText);

        await dbContext.ExchangeLogs.AddAsync(log, cancellationToken);
    }
}
