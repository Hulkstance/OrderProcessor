using System.Threading.Channels;
using OrderService.Domain.Accounts;

namespace OrderService.Infrastructure.PartnerIntegrations.Subscriptions;

internal sealed class AccountSubscriptionQueue
{
    private readonly Channel<Account> _channel = Channel.CreateUnbounded<Account>(
        new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });

    public void Enqueue(Account account) => _channel.Writer.TryWrite(account);

    public IAsyncEnumerable<Account> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
