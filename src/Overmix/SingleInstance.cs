namespace Overmix;

/// <summary>
/// Keeps Overmix to one copy. Starting it again asks the running copy to show its Panel, which matters
/// because it lives in the tray and has no taskbar button.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string Name = @"Local\Overmix-3d1f0c52";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _showPanel = new(false, EventResetMode.AutoReset, Name + "-show-panel");

    public SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, Name, out var createdNew);
        IsFirst = createdNew;
    }

    public bool IsFirst { get; }

    public void AskFirstToShowPanel() => _showPanel.Set();

    /// <summary>Calls <paramref name="show"/> on a background thread whenever another copy asks. It waits without using the CPU.</summary>
    public void ListenForShowPanel(Action show)
    {
        var listener = new Thread(() =>
        {
            try
            {
                while (_showPanel.WaitOne()) show();
            }
            catch (ObjectDisposedException)
            {
                // Overmix is quitting.
            }
        })
        {
            IsBackground = true,
            Name = "Overmix single instance",
        };
        listener.Start();
    }

    public void Dispose()
    {
        if (IsFirst) _mutex.ReleaseMutex();
        _mutex.Dispose();
        _showPanel.Dispose();
    }
}
