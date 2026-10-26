using System.Diagnostics;
using System.Runtime.InteropServices;
using Overmix.Core.Sound;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Notices which program fills the main screen, using Windows' "foreground window changed" event. Games often
/// reach full size a moment after they appear, so each change is looked at again a little later.
/// </summary>
internal sealed unsafe class WindowsMainScreen : IMainScreen, IDisposable
{
    private static readonly TimeSpan[] LookAgainAfter = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];
    private static readonly HashSet<string> ShellClasses = ["Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd"];
    private static readonly HashSet<string> ShellPrograms =
        new(["explorer", "LockApp", "SearchHost", "StartMenuExperienceHost", "ShellExperienceHost"], StringComparer.OrdinalIgnoreCase);

    private static WindowsMainScreen? _instance;

    private readonly SynchronizationContext _ui;
    private readonly nint _hook;
    private readonly Timer _lookAgain;
    private readonly ProcessNames _names = new();
    private readonly Dictionary<ProgramName, int> _processes = [];
    private int _lookAgainStep;

    public WindowsMainScreen(SynchronizationContext ui)
    {
        _ui = ui;
        _instance = this;
        _lookAgain = new Timer(_ => _ui.Post(_ => LookAgain(), null));
        _hook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, 0, &OnForegroundChanged, 0, 0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        Look();
    }

    public FullScreenProgram? Current { get; private set; }

    public event EventHandler<FullScreenProgram>? Filled;

    public event EventHandler<ProgramName>? ProgramQuit;

    public void Dispose()
    {
        UnhookWinEvent(_hook);
        _lookAgain.Dispose();
        _instance = null;
    }

    /// <summary>Windows calls this on the UI thread, which set the hook and runs its message loop.</summary>
    [UnmanagedCallersOnly]
    private static void OnForegroundChanged(nint hook, uint eventType, nint window, int objectId, int childId, uint thread, uint time)
    {
        try
        {
            _instance?.ForegroundChanged();
        }
        catch (Exception e)
        {
            Trace.TraceError($"Overmix main screen: {e}");
        }
    }

    private void ForegroundChanged()
    {
        Look();
        _lookAgainStep = 0;
        _lookAgain.Change(LookAgainAfter[0], Timeout.InfiniteTimeSpan);
    }

    private void LookAgain()
    {
        Look();
        if (++_lookAgainStep < LookAgainAfter.Length)
            _lookAgain.Change(LookAgainAfter[_lookAgainStep] - LookAgainAfter[_lookAgainStep - 1], Timeout.InfiniteTimeSpan);
    }

    private void Look()
    {
        var filling = FullScreenProgramNow();
        if (filling != Current)
        {
            Current = filling;
            if (filling is not null) Filled?.Invoke(this, filling);
        }

        NoticeQuits();
    }

    private FullScreenProgram? FullScreenProgramNow()
    {
        var window = GetForegroundWindow();
        if (window == 0 || IsZoomed(window) || !FillsMainScreen(window) || ShellClasses.Contains(WindowClass(window))) return null;

        GetWindowThreadProcessId(window, out var processId);
        if (processId == 0 || processId == Environment.ProcessId) return null;
        if (_names.Find((int)processId) is not { } name || ShellPrograms.Contains(name)) return null;

        var program = new ProgramName(name);
        _processes[program] = (int)processId;
        var title = WindowText(window).Trim();
        return new FullScreenProgram(program, title.Length > 0 ? title : name);
    }

    private void NoticeQuits()
    {
        if (_processes.Count == 0) return;
        var running = _names.Running();
        foreach (var (program, processId) in _processes.ToList())
        {
            if (running.Contains(processId)) continue;
            _processes.Remove(program);
            if (Current?.Program == program) Current = null;
            ProgramQuit?.Invoke(this, program);
        }
    }

    private static bool FillsMainScreen(nint window)
    {
        if (!GetWindowRect(window, out var bounds)) return false;
        var monitor = MonitorFromWindow(window, MONITOR_DEFAULTTONULL);
        var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (monitor == 0 || !GetMonitorInfo(monitor, ref info) || (info.dwFlags & MONITORINFOF_PRIMARY) == 0) return false;

        var screen = info.rcMonitor;
        return bounds.Left <= screen.Left && bounds.Top <= screen.Top && bounds.Right >= screen.Right && bounds.Bottom >= screen.Bottom;
    }
}
