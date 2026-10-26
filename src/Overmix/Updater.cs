using System.Diagnostics;
using System.Reflection;
using Velopack;

namespace Overmix;

/// <summary>
/// Keeps an installed Overmix up to date from the folder build\pack.ps1 packs releases into. It looks at
/// start-up and whenever that folder changes, so a new build installs itself within seconds of being packed.
/// Between builds it does nothing at all.
/// </summary>
internal sealed class Updater : IDisposable
{
    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RetryWhileBusy = TimeSpan.FromSeconds(20);

    private readonly UpdateManager? _manager;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer _timer;
    private readonly Func<bool> _canRestartNow;
    private int _checking;

    /// <param name="canRestartNow">False while restarting would get in the way, such as while the Overlay is open.</param>
    public Updater(Func<bool> canRestartNow)
    {
        _canRestartNow = canRestartNow;
        _timer = new Timer(_ => _ = CheckAsync());
        var source = typeof(Updater).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "OvermixUpdateSource")?.Value;
        if (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source)) return;

        try
        {
            var manager = new UpdateManager(source);
            if (!manager.IsInstalled) return; // Running from a build folder, not an install.
            _manager = manager;
            _watcher = new FileSystemWatcher(source, "*.json") { NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite };
            _watcher.Changed += (_, _) => CheckSoon();
            _watcher.Created += (_, _) => CheckSoon();
            _watcher.Renamed += (_, _) => CheckSoon();
            _watcher.EnableRaisingEvents = true;
            CheckSoon();
        }
        catch (Exception e) when (e is IOException or ArgumentException or InvalidOperationException)
        {
            Trace.TraceWarning($"Overmix updates: off: {e.Message}");
        }
    }

    public string Version =>
        _manager?.CurrentVersion?.ToString() ?? typeof(Updater).Assembly.GetName().Version?.ToString(3) ?? "dev";

    public bool IsAvailable => _manager is not null;

    /// <summary>Looks for a newer build now.</summary>
    public void CheckNow() => _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);

    public void Dispose()
    {
        _watcher?.Dispose();
        _timer.Dispose();
    }

    private void CheckSoon() => _timer.Change(SettleTime, Timeout.InfiniteTimeSpan);

    private async Task CheckAsync()
    {
        if (_manager is null || Interlocked.Exchange(ref _checking, 1) == 1) return;
        try
        {
            if (await _manager.CheckForUpdatesAsync() is not { } update) return;
            await _manager.DownloadUpdatesAsync(update);
            if (!_canRestartNow())
            {
                _timer.Change(RetryWhileBusy, Timeout.InfiniteTimeSpan);
                return;
            }

            Trace.TraceInformation($"Overmix updates: installing {update.TargetFullRelease.Version} and restarting");
            _manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
        }
        catch (Exception e)
        {
            Trace.TraceWarning($"Overmix updates: {e.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _checking, 0);
        }
    }
}
