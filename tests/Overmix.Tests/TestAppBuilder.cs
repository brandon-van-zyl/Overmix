using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Overmix.Tests.TestAppBuilder))]

namespace Overmix.Tests;

/// <summary>Runs Overmix's UI without a screen, drawing for real so tests can save screenshots.</summary>
public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<OvermixApplication>()
            .UseSkia()
            .UseHarfBuzz()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
