using System.Diagnostics;
using System.Runtime.InteropServices;
using Overmix.Core.Shortcuts;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Shortcuts through Windows' RegisterHotKey, delivered to a hidden message-only window on the UI thread.
/// Windows does all the listening, so this costs nothing until a Shortcut is pressed.
/// </summary>
internal sealed unsafe class WindowsShortcutKeys : IShortcutKeys, IDisposable
{
    private static WindowsShortcutKeys? _instance;

    private readonly nint _window;
    private readonly Dictionary<Shortcut, int> _ids = [];
    private readonly Dictionary<int, Shortcut> _shortcuts = [];
    private int _nextId = 1;

    public WindowsShortcutKeys()
    {
        _instance = this;
        fixed (char* name = "OvermixShortcuts")
        {
            var module = GetModuleHandle(null);
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WindowProc,
                hInstance = module,
                lpszClassName = name,
            };
            RegisterClassEx(&windowClass);
            _window = CreateWindowEx(0, name, name, 0, 0, 0, 0, 0, HWND_MESSAGE, 0, module, null);
        }

        if (_window == 0) Trace.TraceError($"Overmix shortcuts: no message window (error {Marshal.GetLastPInvokeError()})");
    }

    public event EventHandler<Shortcut>? Pressed;

    public bool Register(Shortcut shortcut)
    {
        if (_ids.ContainsKey(shortcut)) return true;
        if (_window == 0 || !KeyCodes.TryGet(shortcut.Key, out var virtualKey)) return false;

        var id = _nextId++;
        if (!RegisterHotKey(_window, id, ToModifiers(shortcut.Modifiers) | MOD_NOREPEAT, virtualKey))
        {
            Trace.TraceWarning($"Overmix shortcuts: {shortcut} is taken (error {Marshal.GetLastPInvokeError()})");
            return false;
        }

        _ids[shortcut] = id;
        _shortcuts[id] = shortcut;
        return true;
    }

    public void Unregister(Shortcut shortcut)
    {
        if (!_ids.Remove(shortcut, out var id)) return;
        UnregisterHotKey(_window, id);
        _shortcuts.Remove(id);
    }

    public void Dispose()
    {
        foreach (var shortcut in _ids.Keys.ToList()) Unregister(shortcut);
        if (_window != 0) DestroyWindow(_window);
        _instance = null;
    }

    private static uint ToModifiers(ShortcutModifiers modifiers)
    {
        var result = 0u;
        if (modifiers.HasFlag(ShortcutModifiers.Ctrl)) result |= MOD_CONTROL;
        if (modifiers.HasFlag(ShortcutModifiers.Alt)) result |= MOD_ALT;
        if (modifiers.HasFlag(ShortcutModifiers.Shift)) result |= MOD_SHIFT;
        if (modifiers.HasFlag(ShortcutModifiers.Win)) result |= MOD_WIN;
        return result;
    }

    [UnmanagedCallersOnly]
    private static nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        if (message == WM_HOTKEY && _instance is { } keys && keys._shortcuts.TryGetValue((int)wParam, out var shortcut))
        {
            try
            {
                keys.Pressed?.Invoke(keys, shortcut);
            }
            catch (Exception e)
            {
                // An exception can't cross back into Windows here; it would end the app.
                Trace.TraceError($"Overmix shortcut {shortcut}: {e}");
            }

            return 0;
        }

        return DefWindowProc(window, message, wParam, lParam);
    }
}
