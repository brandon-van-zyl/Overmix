using Overmix.Core;

namespace Overmix.WindowsPlatform;

public static class WindowsPlatform
{
    /// <summary>On the start-with-Windows entry, so Overmix can tell Windows started it at sign-in.</summary>
    public const string StartedAtSignInArgument = "--at-sign-in";

    /// <summary>
    /// Creates the Windows parts of Overmix. Call it on the UI thread once its message loop is running,
    /// passing that thread's context; every part raises its events there. Dispose the result on exit.
    /// </summary>
    public static (Platform Platform, IDisposable Parts) Create(SynchronizationContext ui)
    {
        var audio = new AudioWorker(ui);
        var mainScreen = new WindowsMainScreen(ui);
        var shortcutKeys = new WindowsShortcutKeys(ui);
        var nowPlaying = new WindowsNowPlaying(ui);
        var icons = new WindowsProgramIcons();
        var muteBorder = new WindowsMuteBorder();
        var platform = new Platform(
            new WindowsProgramSound(audio),
            mainScreen,
            new WindowsMicrophones(audio),
            new WindowsSoundOutputs(audio),
            nowPlaying,
            shortcutKeys,
            new WindowsStartWithWindows(),
            new WindowsGameFocus(),
            icons,
            new WindowsMuteChime(),
            muteBorder);
        return (platform, new Parts([muteBorder, icons, nowPlaying, shortcutKeys, mainScreen, audio]));
    }

    /// <summary>Stops Overmix starting with Windows. Used when it's uninstalled.</summary>
    public static void RemoveStartWithWindows() => new WindowsStartWithWindows().Set(false);

    /// <summary>Shows an error in a plain Windows message box, which works even when the UI couldn't start.</summary>
    public static void ShowError(string message) => NativeMethods.MessageBox(0, message, "Overmix", NativeMethods.MB_ICONERROR);

    private sealed class Parts(IDisposable[] parts) : IDisposable
    {
        public void Dispose()
        {
            foreach (var part in parts) part.Dispose();
        }
    }
}
