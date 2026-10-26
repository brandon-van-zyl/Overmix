using Overmix.Core;

namespace Overmix.WindowsPlatform;

public static class WindowsPlatform
{
    /// <summary>
    /// Creates the Windows parts of Overmix. Call it on the UI thread once its message loop is running,
    /// passing that thread's context; every part raises its events there. Dispose the result on exit.
    /// </summary>
    public static (Platform Platform, IDisposable Parts) Create(SynchronizationContext ui)
    {
        var audio = new AudioWorker(ui);
        var mainScreen = new WindowsMainScreen(ui);
        var shortcutKeys = new WindowsShortcutKeys();
        var nowPlaying = new WindowsNowPlaying(ui);
        var icons = new WindowsProgramIcons();
        var platform = new Platform(
            new WindowsProgramSound(audio),
            mainScreen,
            new WindowsMicrophones(audio),
            new WindowsSoundOutputs(audio),
            nowPlaying,
            shortcutKeys,
            new WindowsStartWithWindows(),
            new WindowsGameFocus(),
            icons);
        return (platform, new Parts([icons, nowPlaying, shortcutKeys, mainScreen, audio]));
    }

    /// <summary>Stops Overmix starting with Windows. Used when it's uninstalled.</summary>
    public static void RemoveStartWithWindows() => new WindowsStartWithWindows().Set(false);

    private sealed class Parts(IDisposable[] parts) : IDisposable
    {
        public void Dispose()
        {
            foreach (var part in parts) part.Dispose();
        }
    }
}
