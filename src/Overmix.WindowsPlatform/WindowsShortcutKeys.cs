using System.Diagnostics;
using System.Runtime.InteropServices;
using Overmix.Core.Shortcuts;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Listens for Shortcuts two ways: a keyboard hook, which catches them even when another app has claimed the
/// same keys or a game turns hotkeys off, and Windows' RegisterHotKey, which still works while an app running
/// as administrator has focus (the hook can't see keys then). Neither does anything until a key is pressed.
/// </summary>
internal sealed unsafe class WindowsShortcutKeys : IShortcutKeys, IDisposable
{
    /// <summary>One press can arrive both ways; count it once.</summary>
    private const long SamePressMilliseconds = 300;

    private static WindowsShortcutKeys? _instance;

    private readonly KeyboardHook? _hook;
    private readonly nint _window;
    private readonly HashSet<Shortcut> _listening = [];
    private readonly Dictionary<Shortcut, int> _hotkeyIds = [];
    private readonly Dictionary<int, Shortcut> _hotkeys = [];
    private readonly Dictionary<Shortcut, long> _lastPressed = [];
    private int _nextId = 1;

    /// <param name="ui">Where Pressed is raised. Without it there's no hook: only Windows' hotkeys.</param>
    public WindowsShortcutKeys(SynchronizationContext? ui, bool useHook = true)
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
        if (useHook && ui is not null) _hook = new KeyboardHook(shortcut => ui.Post(_ => Fire(shortcut), null));
    }

    public event EventHandler<Shortcut>? Pressed;

    public bool Register(Shortcut shortcut)
    {
        if (!KeyCodes.TryGet(shortcut.Key, out var virtualKey)) return false;
        if (_listening.Add(shortcut)) _hook?.SetShortcuts(_listening);
        if (!_hotkeyIds.ContainsKey(shortcut)) RegisterHotKey(shortcut, virtualKey);
        return _hotkeyIds.ContainsKey(shortcut) || _hook?.IsActive == true;
    }

    public void Unregister(Shortcut shortcut)
    {
        if (_listening.Remove(shortcut)) _hook?.SetShortcuts(_listening);
        if (!_hotkeyIds.Remove(shortcut, out var id)) return;
        UnregisterHotKey(_window, id);
        _hotkeys.Remove(id);
    }

    public void Dispose()
    {
        foreach (var shortcut in _listening.ToList()) Unregister(shortcut);
        _hook?.Dispose();
        if (_window != 0) DestroyWindow(_window);
        _instance = null;
    }

    private void RegisterHotKey(Shortcut shortcut, uint virtualKey)
    {
        if (_window == 0) return;
        var id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_window, id, ToModifiers(shortcut.Modifiers) | MOD_NOREPEAT, virtualKey))
        {
            var catcher = _hook?.IsActive == true ? "the keyboard hook catches it instead" : "it won't work";
            Trace.TraceWarning($"Overmix shortcuts: another app has {shortcut}, so {catcher} (error {Marshal.GetLastPInvokeError()})");
            return;
        }

        _hotkeyIds[shortcut] = id;
        _hotkeys[id] = shortcut;
    }

    private void Fire(Shortcut shortcut)
    {
        var now = Environment.TickCount64;
        if (_lastPressed.TryGetValue(shortcut, out var last) && now - last < SamePressMilliseconds) return;
        _lastPressed[shortcut] = now;
        Trace.TraceInformation($"Overmix shortcut: {shortcut}");
        Pressed?.Invoke(this, shortcut);
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
        if (message == WM_HOTKEY && _instance is { } keys && keys._hotkeys.TryGetValue((int)wParam, out var shortcut))
        {
            try
            {
                keys.Fire(shortcut);
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
