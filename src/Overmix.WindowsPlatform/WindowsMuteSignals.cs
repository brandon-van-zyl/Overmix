using System.Runtime.InteropServices;
using Overmix.Core.Voice;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.WindowsPlatform;

/// <summary>The Mute Chime: two quick notes, falling when the mic mutes and rising when it goes live.</summary>
internal sealed unsafe class WindowsMuteChime : IMuteChime
{
    // On the pinned heap, because Windows keeps reading a sound from memory after PlaySound returns.
    private static readonly nint Falling = Pin(ChimeSound.Notes(660, 440));
    private static readonly nint Rising = Pin(ChimeSound.Notes(440, 660));

    public void Play(bool muted) => PlaySound((void*)(muted ? Falling : Rising), 0, SND_MEMORY | SND_ASYNC | SND_NODEFAULT);

    private static nint Pin(byte[] sound)
    {
        var pinned = GC.AllocateUninitializedArray<byte>(sound.Length, pinned: true);
        sound.CopyTo(pinned, 0);
        return Marshal.UnsafeAddrOfPinnedArrayElement(pinned, 0);
    }
}

/// <summary>Builds short sounds of pure notes as WAV files in memory.</summary>
internal static class ChimeSound
{
    private const int SampleRate = 44_100;
    private const double NoteSeconds = 0.075;
    private const double FadeSeconds = 0.008;
    private const double Loudness = 0.25;

    public static byte[] Notes(params double[] frequencies)
    {
        var perNote = (int)(SampleRate * NoteSeconds);
        var samples = new short[perNote * frequencies.Length];
        for (var note = 0; note < frequencies.Length; note++)
        {
            for (var i = 0; i < perNote; i++)
            {
                var time = (double)i / SampleRate;

                // Fade each note in and out so it doesn't click; a soft overtone keeps it from sounding harsh.
                var envelope = Math.Min(1, Math.Min(time, NoteSeconds - time) / FadeSeconds);
                var wave = Math.Sin(2 * Math.PI * frequencies[note] * time) + 0.3 * Math.Sin(4 * Math.PI * frequencies[note] * time);
                samples[note * perNote + i] = (short)(wave / 1.3 * envelope * Loudness * short.MaxValue);
            }
        }

        return Wav(samples);
    }

    private static byte[] Wav(short[] samples)
    {
        var dataBytes = samples.Length * sizeof(short);
        using var stream = new MemoryStream(44 + dataBytes);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(SampleRate);
        writer.Write(SampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        foreach (var sample in samples) writer.Write(sample);
        writer.Flush();
        return stream.ToArray();
    }
}

/// <summary>
/// The Mute Border as thin red strips along the edges of every screen. They're plain Windows windows that
/// clicks pass straight through and that never take focus, so the game keeps it. Solid strips rather than one
/// see-through window over each screen, because that is what wallhack overlays look like to anti-cheat. They
/// draw once as they appear and cost nothing after.
/// </summary>
internal sealed unsafe class WindowsMuteBorder : IMuteBorder, IDisposable
{
    private const string ClassName = "OvermixMuteBorder";
    private const int Thickness = 5;
    private const uint Red = 0x005E3FF4; // #F43F5E, written the way Windows stores colours: blue, green, red.

    private static readonly List<(RECT Bounds, uint Dpi)> FoundScreens = [];

    private readonly List<nint> _strips = [];
    private nint _module;
    private nint _brush;
    private bool _classReady;

    /// <summary>How many strips are on screen, for tests.</summary>
    public int StripCount => _strips.Count;

    public void Show()
    {
        // Rebuilt each time, in case screens were added, moved or rescaled since.
        Hide();
        if (!EnsureClass()) return;
        foreach (var (bounds, dpi) in Screens())
        {
            var thickness = Math.Max(2, (int)(Thickness * dpi / 96));
            foreach (var strip in StripsAround(bounds, thickness)) AddStrip(strip);
        }
    }

    public void Hide()
    {
        foreach (var strip in _strips) DestroyWindow(strip);
        _strips.Clear();
    }

    public void Dispose()
    {
        Hide();
        if (_brush != 0) DeleteObject(_brush);
    }

    private static RECT[] StripsAround(RECT screen, int thickness) =>
    [
        new() { Left = screen.Left, Top = screen.Top, Right = screen.Right, Bottom = screen.Top + thickness },
        new() { Left = screen.Left, Top = screen.Bottom - thickness, Right = screen.Right, Bottom = screen.Bottom },
        new() { Left = screen.Left, Top = screen.Top + thickness, Right = screen.Left + thickness, Bottom = screen.Bottom - thickness },
        new() { Left = screen.Right - thickness, Top = screen.Top + thickness, Right = screen.Right, Bottom = screen.Bottom - thickness },
    ];

    private void AddStrip(RECT bounds)
    {
        nint strip;
        fixed (char* name = ClassName)
        {
            strip = CreateWindowEx(
                WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TRANSPARENT,
                name, null, WS_POPUP, bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top,
                0, 0, _module, null);
        }

        if (strip == 0) return;
        SetLayeredWindowAttributes(strip, 0, 255, LWA_ALPHA);
        ShowWindow(strip, SW_SHOWNOACTIVATE);
        _strips.Add(strip);
    }

    private bool EnsureClass()
    {
        if (_classReady) return true;
        _module = GetModuleHandle(null);
        _brush = CreateSolidBrush(Red);
        fixed (char* name = ClassName)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WindowProc,
                hInstance = _module,
                hbrBackground = _brush,
                lpszClassName = name,
            };
            _classReady = RegisterClassEx(&windowClass) != 0;
        }

        return _classReady;
    }

    private static List<(RECT Bounds, uint Dpi)> Screens()
    {
        FoundScreens.Clear();
        EnumDisplayMonitors(0, null, &OnScreen, 0);
        return [.. FoundScreens];
    }

    [UnmanagedCallersOnly]
    private static int OnScreen(nint monitor, nint deviceContext, RECT* bounds, nint data)
    {
        var dpi = GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0 ? dpiX : 96u;
        FoundScreens.Add((*bounds, dpi));
        return 1;
    }

    [UnmanagedCallersOnly]
    private static nint WindowProc(nint window, uint message, nint wParam, nint lParam) => DefWindowProc(window, message, wParam, lParam);
}
