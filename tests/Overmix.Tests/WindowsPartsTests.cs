using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Overmix.Core.Shortcuts;
using Overmix.WindowsPlatform;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.Tests;

/// <summary>
/// Checks the real Windows parts, pressing keys for real. These run on GitHub's Windows machine before every
/// release. They skip themselves anywhere else, or where Windows won't take pretend key presses.
/// </summary>
public sealed unsafe partial class WindowsPartsTests
{
    private const uint InputKeyboard = 1;
    private const uint KeyUp = 0x0002;

    private const int OtherAppsHotkey = 0xB0B;

    private static readonly Shortcut TestShortcut = Shortcut.Parse("Ctrl+Alt+Shift+F11");

    [Fact]
    public void The_keyboard_hook_catches_a_Shortcut()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Needs Windows.");
        using var pressed = new ManualResetEventSlim();
        using var hook = new KeyboardHook(shortcut =>
        {
            if (shortcut == TestShortcut) pressed.Set();
        });
        Assert.True(hook.IsActive, "The keyboard hook didn't install.");
        hook.SetShortcuts([TestShortcut]);

        PressOrSkip(TestShortcut);

        Assert.True(pressed.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken), "The hook never saw the Shortcut.");
    }

    [Fact]
    public void A_Shortcut_registered_with_Windows_fires()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Needs Windows.");
        using var ready = new ManualResetEventSlim();
        using var pressed = new ManualResetEventSlim();
        var registered = false;
        var threadId = 0u;
        Exception? failure = null;

        // Windows delivers hotkeys through the message loop of the thread that registered them.
        var thread = new Thread(() =>
        {
            try
            {
                threadId = GetCurrentThreadId();
                using var keys = new WindowsShortcutKeys(ui: null);
                keys.Pressed += (_, shortcut) =>
                {
                    if (shortcut == TestShortcut) pressed.Set();
                };
                registered = keys.Register(TestShortcut);
                ready.Set();
                MSG message;
                while (GetMessage(&message, 0, 0, 0) > 0)
                {
                    TranslateMessage(&message);
                    DispatchMessage(&message);
                }
            }
            catch (Exception e)
            {
                failure = e;
                ready.Set();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        try
        {
            Assert.True(ready.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken), "The hotkey thread didn't start.");
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
            Assert.True(registered, "Windows refused the hotkey.");

            PressOrSkip(TestShortcut);

            Assert.True(pressed.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken), "The hotkey never fired.");
        }
        finally
        {
            PostThreadMessage(threadId, WM_QUIT, 0, 0);
            thread.Join(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public void Only_the_keyboard_hook_catches_a_Shortcut_another_app_holds()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Needs Windows.");
        Assert.True(KeyCodes.TryGet(TestShortcut.Key, out var key));

        // A hotkey registered without a window belongs to this thread, which stands in for the other app.
        Assert.True(RegisterHotKey(0, OtherAppsHotkey, MOD_CONTROL | MOD_ALT | MOD_SHIFT, key), "Couldn't hold the Shortcut like another app.");
        try
        {
            using var keys = new WindowsShortcutKeys(new SynchronizationContext());

            keys.UseKeyboardHook = true;
            Assert.True(keys.Register(TestShortcut), "With the hook on, the Shortcut should still work.");
            keys.Unregister(TestShortcut);

            keys.UseKeyboardHook = false;
            Assert.False(keys.UseKeyboardHook);
            Assert.False(keys.Register(TestShortcut), "With the hook off, the other app keeps the Shortcut.");
        }
        finally
        {
            UnregisterHotKey(0, OtherAppsHotkey);
        }
    }

    [Fact]
    public void Finds_a_programs_file_without_opening_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Needs Windows.");

        Assert.Equal(Environment.ProcessPath, ProcessFiles.PathOf(Environment.ProcessId), ignoreCase: true);
    }

    [Fact]
    public void Finds_the_file_of_a_program_Windows_runs_for_itself()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Needs Windows.");
        var services = Process.GetProcessesByName("svchost");
        try
        {
            Assert.SkipWhen(services.Length == 0, "No Windows services here.");
            Assert.Equal(Path.Combine(Environment.SystemDirectory, "svchost.exe"), ProcessFiles.PathOf(services[0].Id), ignoreCase: true);
        }
        finally
        {
            foreach (var service in services) service.Dispose();
        }
    }

    [Fact]
    public void The_Mute_Border_puts_four_strips_on_every_screen()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Needs Windows.");
        using var border = new WindowsMuteBorder();

        border.Show();
        Assert.SkipWhen(border.StripCount == 0, "No screens here.");
        Assert.Equal(0, border.StripCount % 4);

        border.Hide();
        Assert.Equal(0, border.StripCount);
    }

    [Fact]
    public void The_Mute_Chime_is_a_short_WAV_sound()
    {
        var sound = ChimeSound.Notes(660, 440);

        Assert.Equal("RIFF"u8.ToArray(), sound[..4]);
        Assert.Equal("WAVE"u8.ToArray(), sound[8..12]);
        Assert.Equal(sound.Length - 8, BitConverter.ToInt32(sound, 4));
        Assert.InRange((sound.Length - 44) / 2.0 / 44_100, 0.1, 0.2);
    }

    private static void PressOrSkip(Shortcut shortcut)
    {
        Assert.True(KeyCodes.TryGet(shortcut.Key, out var key));
        List<ushort> keys = [];
        if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Ctrl)) keys.Add(VK_CONTROL);
        if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Alt)) keys.Add(VK_MENU);
        if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Shift)) keys.Add(VK_SHIFT);
        keys.Add((ushort)key);

        // Every key down in order, then up in reverse, like a person pressing them.
        var inputs = new INPUT[keys.Count * 2];
        for (var i = 0; i < keys.Count; i++)
        {
            inputs[i] = new INPUT { Type = InputKeyboard, VirtualKey = keys[i] };
            inputs[^(i + 1)] = new INPUT { Type = InputKeyboard, VirtualKey = keys[i], Flags = KeyUp };
        }

        fixed (INPUT* first = inputs)
        {
            var sent = SendInput((uint)inputs.Length, first, sizeof(INPUT));
            Assert.SkipWhen(sent != inputs.Length, "Windows won't take pretend key presses here, as when nobody is signed in.");
        }
    }

    [LibraryImport("user32.dll")]
    private static partial uint SendInput(uint count, INPUT* inputs, int size);

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public uint Type;

        [FieldOffset(8)]
        public ushort VirtualKey;

        [FieldOffset(12)]
        public uint Flags;
    }
}
