using System.Diagnostics;

namespace Overmix;

/// <summary>Writes Overmix's warnings and errors to a small log file next to its settings.</summary>
internal static class Logging
{
    private const long MaxBytes = 1024 * 1024;

    public static string Path { get; } =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Overmix", "overmix.log");

    public static void Start()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            if (File.Exists(Path) && new FileInfo(Path).Length > MaxBytes) File.Delete(Path);
            Trace.Listeners.Add(new TextWriterTraceListener(Path));
            Trace.AutoFlush = true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // No log is better than no Overmix.
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) => Trace.TraceError($"Overmix crashed: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Trace.TraceError($"Overmix background task failed: {e.Exception}");
            e.SetObserved();
        };
        Trace.TraceInformation($"Overmix {typeof(Logging).Assembly.GetName().Version} started {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }
}
