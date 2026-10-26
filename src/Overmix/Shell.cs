using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using Overmix.Core;
using Overmix.Core.Settings;
using Overmix.ViewModels;
using Overmix.Views;
using Overmix.WindowsPlatform;

namespace Overmix;

/// <summary>Puts Overmix together on Windows and runs its Panel, Overlay and tray icon.</summary>
internal sealed class Shell
{
    private static readonly WindowIcon NormalIcon = LoadIcon("overmix.ico");
    private static readonly WindowIcon MutedIcon = LoadIcon("overmix-muted.ico");

    private readonly OvermixCore _core;
    private readonly IDisposable _platform;
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly Updater _updater;
    private readonly PanelWindow _panel;
    private readonly OverlayWindow _overlay;
    private readonly TrayIcon _tray;
    private readonly NativeMenuItem _panelItem = new("Show Panel");

    private Shell(OvermixCore core, IDisposable platform, IClassicDesktopStyleApplicationLifetime desktop)
    {
        _core = core;
        _platform = platform;
        _desktop = desktop;
        _overlay = new OverlayWindow(core.Platform.GameFocus, core.OverlaySpot);
        _updater = new Updater(canRestartNow: () => Dispatcher.UIThread.Invoke(() => !_overlay.IsVisible));

        var viewModel = new OvermixViewModel(core, _updater.Version);
        _panel = new PanelWindow { DataContext = viewModel };
        _overlay.DataContext = viewModel;

        _panel.HideRequested += (_, _) => HidePanel();
        _overlay.Moved += (_, _) => _core.OverlaySpot = _overlay.Spot;
        core.Shortcuts.PopupPressed += (_, _) => _overlay.Toggle();
        core.MicMute.Changed += (_, _) => ShowMicMute();

        _tray = CreateTrayIcon();
        ShowMicMute();
        if (core.PanelOpen) ShowPanel();
        else UpdatePanelItem();
    }

    public static Shell Start(IClassicDesktopStyleApplicationLifetime desktop)
    {
        Logging.Start();
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var ui = SynchronizationContext.Current ?? new AvaloniaSynchronizationContext();
        var (platform, parts) = WindowsPlatform.WindowsPlatform.Create(ui);
        var core = new OvermixCore(platform, new JsonSettingsStore(JsonSettingsStore.DefaultPath));

        // Starting with Windows is the point of a tray app, so it's on from the first install. Settings can turn it off.
        if (Program.IsFirstRun) core.StartsWithWindows = true;
        var shell = new Shell(core, parts, desktop);
        Program.Instance?.ListenForShowPanel(() => Dispatcher.UIThread.Post(shell.ShowPanel));
        return shell;
    }

    private void ShowPanel()
    {
        _panel.ShowAt(_core.PanelSpot);
        _core.PanelOpen = true;
        UpdatePanelItem();
    }

    private void HidePanel()
    {
        _core.PanelSpot = _panel.Spot;
        _panel.Hide();
        _core.PanelOpen = false;
        UpdatePanelItem();
    }

    private void TogglePanel()
    {
        if (_panel.IsVisible) HidePanel();
        else ShowPanel();
    }

    private void UpdatePanelItem() => _panelItem.Header = _panel.IsVisible ? "Hide Panel" : "Show Panel";

    /// <summary>The tray icon turns red with the Panel, so Mic Mute shows even with the Panel hidden.</summary>
    private void ShowMicMute()
    {
        _tray.Icon = _core.MicMute.IsOn ? MutedIcon : NormalIcon;
        _tray.ToolTipText = _core.MicMute.IsOn ? "Overmix: mic muted" : "Overmix";
    }

    private TrayIcon CreateTrayIcon()
    {
        _panelItem.Click += (_, _) => TogglePanel();
        var updates = new NativeMenuItem("Check for updates") { IsEnabled = _updater.IsAvailable };
        updates.Click += (_, _) => _updater.CheckNow();
        var quit = new NativeMenuItem("Quit Overmix");
        quit.Click += (_, _) => Quit();

        var menu = new NativeMenu();
        menu.Items.Add(_panelItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(updates);
        menu.Items.Add(quit);

        var tray = new TrayIcon { Icon = NormalIcon, ToolTipText = "Overmix", Menu = menu };
        tray.Clicked += (_, _) => TogglePanel();
        TrayIcon.SetIcons(Application.Current!, [tray]);
        return tray;
    }

    private void Quit()
    {
        if (_panel.IsVisible) _core.PanelSpot = _panel.Spot;
        _tray.IsVisible = false;
        _updater.Dispose();
        _platform.Dispose();
        _overlay.Close();
        _panel.Quit();
        _desktop.Shutdown();
    }

    private static WindowIcon LoadIcon(string name) => new(AssetLoader.Open(new Uri($"avares://Overmix/Assets/{name}")));
}
