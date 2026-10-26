using Avalonia;
using Velopack;

namespace Overmix;

internal static class Program
{
    /// <summary>The running Overmix's claim to be the only one.</summary>
    public static SingleInstance? Instance { get; private set; }

    /// <summary>True the first time Overmix runs after being installed.</summary>
    public static bool IsFirstRun { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        // First thing, always: lets the installer and updater run their steps, then returns.
        VelopackApp.Build()
            .OnFirstRun(_ => IsFirstRun = true)
            .OnBeforeUninstallFastCallback(_ => WindowsPlatform.WindowsPlatform.RemoveStartWithWindows())
            .Run();

        using var instance = new SingleInstance();
        if (!instance.IsFirst)
        {
            // Overmix is already running, probably in the tray: show its Panel instead.
            instance.AskFirstToShowPanel();
            return;
        }

        Instance = instance;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<OvermixApplication>().UsePlatformDetect().WithInterFont().LogToTrace();
}
