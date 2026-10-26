using Microsoft.Win32;
using Overmix.Core;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.WindowsPlatform;

/// <summary>Starts Overmix at sign-in through the current user's Run key, which is where Task Manager's Startup apps look.</summary>
internal sealed class WindowsStartWithWindows : IStartWithWindows
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Overmix";

    public bool IsOn
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void Set(bool on)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (on) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" {WindowsPlatform.StartedAtSignInArgument}");
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

/// <summary>Remembers the Game's window as the Overlay opens, and gives it focus back when it closes.</summary>
internal sealed class WindowsGameFocus : IGameFocus
{
    private nint _window;

    public void Remember()
    {
        var window = GetForegroundWindow();
        GetWindowThreadProcessId(window, out var processId);
        if (window != 0 && processId != Environment.ProcessId) _window = window;
    }

    public void Return()
    {
        // Allowed because Overmix is in front at this point; Windows refuses focus changes from background apps.
        if (_window != 0 && IsWindow(_window)) SetForegroundWindow(_window);
    }
}
