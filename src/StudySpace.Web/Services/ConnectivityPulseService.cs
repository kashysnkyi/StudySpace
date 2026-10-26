using System.Collections.Concurrent;

namespace StudySpace.Web.Services;

public sealed record ConnectivityPulse(long Version, DateTime Timestamp, int ActiveEditors);

public sealed class ConnectivityPulseService : BackgroundService
{
    private readonly ConcurrentDictionary<long, TaskCompletionSource<ConnectivityPulse>> _waiters = new();
    private long _version;

    public ConnectivityPulse Current => CreatePulse(Interlocked.Read(ref _version));

    public async Task<ConnectivityPulse> WaitForChangeAsync(
        long knownVersion,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var current = Current;
        if (current.Version > knownVersion)
        {
            return current;
        }

        var key = DateTime.UtcNow.Ticks;
        var waiter = new TaskCompletionSource<ConnectivityPulse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiters[key] = waiter;

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            await using var registration = timeoutSource.Token.Register(() => waiter.TrySetResult(Current));
            return await waiter.Task;
        }
        finally
        {
            _waiters.TryRemove(key, out _);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var version = Interlocked.Increment(ref _version);
            var pulse = CreatePulse(version);
            foreach (var waiter in _waiters.Values)
            {
                waiter.TrySetResult(pulse);
            }
        }
    }

    private static ConnectivityPulse CreatePulse(long version) =>
        new(version, DateTime.UtcNow, (int)(version % 7) + 1);
}

