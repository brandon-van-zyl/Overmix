using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Overmix.Core;
using Overmix.Core.Sound;

namespace Overmix.ViewModels;

/// <summary>Turns each program's icon into a bitmap once and hands out the same bitmap after that.</summary>
public sealed class IconCache(OvermixCore core)
{
    private readonly Dictionary<ProgramName, Bitmap> _bitmaps = [];

    public async Task<Bitmap?> GetAsync(ProgramName program)
    {
        if (_bitmaps.TryGetValue(program, out var known)) return known;

        var icon = await core.Icons.GetAsync(program, core.Apps.Find(program)?.Path);
        if (icon is null) return null;
        if (_bitmaps.TryGetValue(program, out known)) return known;

        var bitmap = ToBitmap(icon);
        _bitmaps[program] = bitmap;

        // Remembering where an App lives lets its icon show next time, even before it's running. It changes
        // the App list, so it waits until whatever asked for this icon has finished.
        Dispatcher.UIThread.Post(() => core.RememberPath(program, icon.SourcePath));
        return bitmap;
    }

    private static unsafe Bitmap ToBitmap(ProgramIcon icon)
    {
        fixed (byte* pixels = icon.Pixels)
        {
            return new Bitmap(
                PixelFormat.Bgra8888, AlphaFormat.Premul, (nint)pixels, new PixelSize(icon.Width, icon.Height),
                new Vector(96, 96), icon.Width * 4);
        }
    }
}
