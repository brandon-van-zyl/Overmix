using System.Diagnostics;
using System.Runtime.InteropServices;
using Overmix.Core.Shortcuts;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Sees every key press through a low-level keyboard hook, so Shortcuts work even when another app already
/// claimed the same keys, or a game turns hotkeys off while it has focus. It runs on its own thread and only
/// compares each key with the Shortcuts, so it adds no noticeable delay to typing or playing. A Shortcut's
/// key is kept from the focused app, just as Windows does with its own hotkeys.
/// </summary>
internal sealed unsafe class KeyboardHook : IDisposable
{
    private static KeyboardHook? _instance;

    private readonly Action<Shortcut> _pressed;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _started = new();
    private readonly HashSet<uint> _heldShortcutKeys = [];
    private volatile Dictionary<(uint Key, ShortcutModifiers Modifiers), Shortcut> _shortcuts = [];
    private uint _threadId;
    private nint _hook;

    /// <param name="pressed">Called on the hook's thread for each Shortcut press. It must return quickly.</param>
    public KeyboardHook(Action<Shortcut> pressed)
    {
        _pressed = pressed;
        _instance = this;
        _thread = new Thread(Run) { IsBackground = true, Name = "Overmix keyboard hook" };
        _thread.Start();
        _started.Wait(TimeSpan.FromSeconds(2));
    }

    public bool IsActive => _hook != 0;

    public void SetShortcuts(IEnumerable<Shortcut> shortcuts)
    {
        var byKey = new Dictionary<(uint, ShortcutModifiers), Shortcut>();
        foreach (var shortcut in shortcuts)
        {
            if (KeyCodes.TryGet(shortcut.Key, out var key)) byKey[(key, shortcut.Modifiers)] = shortcut;
        }

        _shortcuts = byKey;
    }

    public void Dispose()
    {
        if (_threadId != 0) PostThreadMessage(_threadId, WM_QUIT, 0, 0);
        _thread.Join(TimeSpan.FromSeconds(2));
        _instance = null;
        _started.Dispose();
    }

    private void Run()
    {
        _threadId = GetCurrentThreadId();
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, &HookProc, GetModuleHandle(null), 0);
        if (_hook == 0) Trace.TraceError($"Overmix keyboard hook: not installed (error {Marshal.GetLastPInvokeError()})");
        _started.Set();
        if (_hook == 0) return;

        // Windows calls the hook through this thread's message loop.
        MSG message;
        while (GetMessage(&message, 0, 0, 0) > 0)
        {
            TranslateMessage(&message);
            DispatchMessage(&message);
        }

        UnhookWindowsHookEx(_hook);
    }

    [UnmanagedCallersOnly]
    private static nint HookProc(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && _instance is { } hook)
        {
            try
            {
                if (hook.Handle((uint)wParam, ((KBDLLHOOKSTRUCT*)lParam)->vkCode)) return 1;
            }
            catch (Exception e)
            {
                // An exception can't cross back into Windows here; it would end the app.
                Trace.TraceError($"Overmix keyboard hook: {e}");
            }
        }

        return CallNextHookEx(0, code, wParam, lParam);
    }

    /// <summary>True to keep the key from the focused app.</summary>
    private bool Handle(uint message, uint key)
    {
        if (message is WM_KEYUP or WM_SYSKEYUP) return _heldShortcutKeys.Remove(key);
        if (message is not (WM_KEYDOWN or WM_SYSKEYDOWN)) return false;

        // Holding a Shortcut down repeats its key; one press is one Shortcut.
        if (_heldShortcutKeys.Contains(key)) return true;
        if (!_shortcuts.TryGetValue((key, ModifiersHeld()), out var shortcut)) return false;
        _heldShortcutKeys.Add(key);
        _pressed(shortcut);
        return true;
    }

    private static ShortcutModifiers ModifiersHeld()
    {
        var modifiers = ShortcutModifiers.None;
        if (IsHeld(VK_CONTROL)) modifiers |= ShortcutModifiers.Ctrl;
        if (IsHeld(VK_MENU)) modifiers |= ShortcutModifiers.Alt;
        if (IsHeld(VK_SHIFT)) modifiers |= ShortcutModifiers.Shift;
        if (IsHeld(VK_LWIN) || IsHeld(VK_RWIN)) modifiers |= ShortcutModifiers.Win;
        return modifiers;
    }

    private static bool IsHeld(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
}
