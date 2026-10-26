using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Overmix.Core;
using Overmix.Fakes;
using Overmix.ViewModels;
using Overmix.Views;

namespace Overmix.Tests;

/// <summary>Saves pictures of every window to bin/.../screenshots, to check how they look without a PC.</summary>
public sealed class Screenshots
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "screenshots");

    [AvaloniaFact]
    public void Panel_Overlay_and_Settings()
    {
        Directory.CreateDirectory(Folder);
        var pc = FakePc.GamingEvening();
        var viewModel = new OvermixViewModel(new OvermixCore(pc.Platform, new InMemorySettingsStore(FakePc.SampleSettings())), "0.1.0-test");

        var panel = new PanelWindow { DataContext = viewModel };
        panel.Show();
        Save(panel, "panel.png");

        viewModel.ToggleMicMuteCommand.Execute(null);
        Save(panel, "panel-muted.png");

        var overlay = new OverlayWindow(pc.GameFocus, spot: null) { DataContext = viewModel };
        overlay.Open();
        Save(overlay, "overlay-muted.png");
        viewModel.ToggleMicMuteCommand.Execute(null);
        Save(overlay, "overlay.png");

        viewModel.ToggleSettingsCommand.Execute(null);
        Save(panel, "settings.png");
    }

    private static void Save(Window window, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Combine(Folder, name), PngBitmapEncoderOptions.Default);
    }
}
