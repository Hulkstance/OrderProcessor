using System.Collections.Concurrent;

namespace OrderService.Infrastructure.PartnerIntegrations;

internal sealed class DataBatcher<T>
{
    private readonly ConcurrentDictionary<string, T> _store = new();
    private readonly Func<Dictionary<string, T>, Task> _callback;
    private readonly Func<T, T, T> _updateFunc;
    private readonly SemaphoreSlim _sem = new(1, 1);
    private readonly TimeSpan _triggerInterval;
    private Task _processingTask = Task.CompletedTask;
    private readonly Lock _locker = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ManualResetEvent _triggerEvent = new(false);

    public DataBatcher(TimeSpan triggerInterval, Func<Dictionary<string, T>, Task> callback, Func<T, T, T>? updateFunc = null)
    {
        _triggerInterval = triggerInterval;
        _callback = callback;
        _updateFunc = updateFunc ?? ((xExisting, xUpdate) => xUpdate);
    }

    public Task StartAsync()
    {
        _processingTask = Task.Run(ProcessAsync);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        await _processingTask;
    }

    public async Task AddAsync(Dictionary<string, T> data)
    {
        await _sem.WaitAsync();
        try
        {
            lock (_locker)
            {
                foreach (var item in data)
                {
                    if (_store.TryGetValue(item.Key, out var existing))
                        _store[item.Key] = _updateFunc(existing, item.Value);
                    else
                        _store[item.Key] = item.Value;
                }
            }

            _triggerEvent.Set();
        }
        finally
        {
            _sem.Release();
        }
    }

    private async Task ProcessAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            _triggerEvent.WaitOne();
            await Task.Delay(_triggerInterval);

            Dictionary<string, T> data;
            lock (_locker)
            {
                data = _store.ToDictionary(x => x.Key, x => x.Value);
                _store.Clear();
            }
            _triggerEvent.Reset();

            if (data.Count > 0)
                await _callback(data);
        }
    }
}
