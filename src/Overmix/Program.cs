using System.Diagnostics;
using Avalonia;
using Velopack;

namespace Overmix;

internal static class Program
{
    /// <summary>The running Overmix's claim to be the only one.</summary>
    public static SingleInstance? Instance { get; private set; }

    /// <summary>True the first time Overmix runs after being installed.</summary>
    public static bool IsFirstRun { get; private set; }

    /// <summary>Windows started Overmix as you signed in, rather than you starting it.</summary>
    public static bool StartedAtSignIn { get; private set; }

    /// <summary>The release build is checking that Overmix starts. It quits by itself after a few seconds.</summary>
    public static bool IsSmokeTest { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        // First thing, always: lets the installer and updater run their steps, then returns.
        VelopackApp.Build()
            .OnFirstRun(_ => IsFirstRun = true)
            .OnBeforeUninstallFastCallback(_ => WindowsPlatform.WindowsPlatform.RemoveStartWithWindows())
            .Run();

        StartedAtSignIn = args.Contains(WindowsPlatform.WindowsPlatform.StartedAtSignInArgument);
        IsSmokeTest = args.Contains("--smoke-test");

        using var instance = new SingleInstance();
        if (!instance.IsFirst)
        {
            // Overmix is already running, probably in the tray: show its Panel instead.
            instance.AskFirstToShowPanel();
            return 0;
        }

        Instance = instance;
        Logging.Start();
        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            // Without this, a failed start would just disappear.
            Trace.TraceError($"Overmix couldn't start: {e}");
            if (!IsSmokeTest)
                WindowsPlatform.WindowsPlatform.ShowError($"Overmix couldn't start:\n\n{e.Message}\n\nThe details are in {Logging.Path}");
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<OvermixApplication>().UsePlatformDetect().WithInterFont().LogToTrace();
}
