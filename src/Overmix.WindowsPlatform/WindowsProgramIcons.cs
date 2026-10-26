using System.Diagnostics;
using System.Runtime.InteropServices;
using Overmix.Core.Sound;
using static Overmix.WindowsPlatform.NativeMethods;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Programs' icons as Windows shows them on their shortcuts, taken from the program file through the shell.
/// Each file is read once; after that the icon comes from memory.
/// </summary>
internal sealed unsafe class WindowsProgramIcons : IProgramIcons, IDisposable
{
    private const int Size = 96;
    private const int SIIGBF_BIGGERSIZEOK = 0x1;
    private const int SIIGBF_ICONONLY = 0x4;

    // The shell expects a single-threaded apartment.
    private readonly ComThread _thread = new("Overmix icons", ApartmentState.STA);
    private readonly Dictionary<string, ProgramIcon> _byPath = new(StringComparer.OrdinalIgnoreCase);

    public Task<ProgramIcon?> GetAsync(ProgramName program, string? knownPath) => _thread.Run(() => Load(program, knownPath));

    public void Dispose() => _thread.Dispose();

    private ProgramIcon? Load(ProgramName program, string? knownPath)
    {
        foreach (var path in Candidates(program, knownPath))
        {
            if (_byPath.TryGetValue(path, out var known)) return known;
            if (Read(path) is not { } icon) continue;
            _byPath[path] = icon;
            return icon;
        }

        return null;
    }

    private static IEnumerable<string> Candidates(ProgramName program, string? knownPath)
    {
        if (!string.IsNullOrWhiteSpace(knownPath) && File.Exists(knownPath)) yield return knownPath;

        foreach (var process in Process.GetProcessesByName(program.Value))
        {
            string? path;
            using (process)
            {
                path = ProcessPath(process.Id);
            }

            if (path is not null) yield return path;
        }
    }

    private static ProgramIcon? Read(string path)
    {
        var factoryId = typeof(IShellItemImageFactory).GUID;
        if (SHCreateItemFromParsingName(path, 0, in factoryId, out var factory) != 0 || factory is null) return null;
        try
        {
            if (factory.GetImage(new Size2D(Size, Size), SIIGBF_BIGGERSIZEOK | SIIGBF_ICONONLY, out var bitmap) != 0) return null;
            try
            {
                return ToIcon(bitmap, path);
            }
            finally
            {
                DeleteObject(bitmap);
            }
        }
        catch (COMException e)
        {
            Trace.TraceWarning($"Overmix icons: {path}: {e.Message}");
            return null;
        }
        finally
        {
            Marshal.ReleaseComObject(factory);
        }
    }

    private static ProgramIcon? ToIcon(nint bitmap, string path)
    {
        BITMAP info;
        if (GetObject(bitmap, sizeof(BITMAP), &info) == 0) return null;
        var width = info.bmWidth;
        var height = Math.Abs(info.bmHeight);

        var header = new BITMAPINFO
        {
            biSize = 40,
            biWidth = width,
            biHeight = -height, // top-down rows
            biPlanes = 1,
            biBitCount = 32,
        };
        var pixels = new byte[width * height * 4];
        var screen = GetDC(0);
        try
        {
            fixed (byte* bits = pixels)
            {
                if (GetDIBits(screen, bitmap, 0, (uint)height, bits, &header, 0) == 0) return null;
            }
        }
        finally
        {
            ReleaseDC(0, screen);
        }

        // Old icons without transparency come back with every alpha byte at zero; show them solid.
        var hasAlpha = false;
        for (var i = 3; i < pixels.Length && !hasAlpha; i += 4) hasAlpha = pixels[i] != 0;
        if (!hasAlpha)
        {
            for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
        }

        return new ProgramIcon(width, height, pixels, path);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        string path, nint bindContext, in Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory? factory);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Size2D(int Width, int Height);

    [ComImport]
    [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(Size2D size, int flags, out nint bitmap);
    }
}
