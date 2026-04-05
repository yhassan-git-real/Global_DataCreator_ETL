namespace GlobalDataCreatorETL.Core.Cancellation;

/// <summary>
/// Thread-safe cancellation manager. Creates and tracks a single active CancellationTokenSource.
/// </summary>
public sealed class CancellationManager
{
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;

    public event EventHandler? CancellationRequested;

    public bool IsOperationActive
    {
        get { lock (_lock) return _cts is not null && !_cts.IsCancellationRequested; }
    }

    public CancellationToken StartNew()
    {
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }
    }

    public void Cancel()
    {
        CancellationTokenSource? cts;
        lock (_lock) cts = _cts;

        if (cts is null || cts.IsCancellationRequested)
            return;

        // Fire cancel on background thread to avoid deadlocks
        Task.Run(() =>
        {
            cts.Cancel();
            CancellationRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Complete()
    {
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = null;
        }
    }
}
