using Avalonia;
using Avalonia.Controls;
using Overmix.Core.Settings;

namespace Overmix.Views;

/// <summary>
/// The Panel: Overmix's main window, kept open on the second screen. Closing or minimising it only
/// hides it to the tray; Quit is in the tray menu.
/// </summary>
public sealed partial class PanelWindow : Window
{
    private bool _quitting;

    public PanelWindow() => InitializeComponent();

    /// <summary>The user closed or minimised the Panel.</summary>
    public event EventHandler? HideRequested;

    /// <summary>Where the Panel sits now, to put it back there next time.</summary>
    public WindowSpot Spot => new(Position.X, Position.Y);

    /// <summary>Shows the Panel where it was last, or on the second screen the first time.</summary>
    public void ShowAt(WindowSpot? spot)
    {
        if (!IsVisible) Position = StartingPosition(spot);
        Show();
        Activate();
    }

    /// <summary>Lets the window really close, when Overmix quits.</summary>
    public void Quit()
    {
        _quitting = true;
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_quitting) return;
        e.Cancel = true;
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // There's no taskbar button to come back to, so minimising hides to the tray instead.
        if (change.Property == WindowStateProperty && WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
            HideRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private PixelPoint StartingPosition(WindowSpot? spot)
    {
        var screens = Screens.All;
        if (spot is not null && screens.Any(screen => screen.WorkingArea.Contains(new PixelPoint(spot.X + 40, spot.Y + 40))))
            return new PixelPoint(spot.X, spot.Y);

        // First run: the top-right corner of the second screen, or of the only one.
        var target = screens.FirstOrDefault(screen => !screen.IsPrimary) ?? Screens.Primary ?? screens.FirstOrDefault();
        if (target is null) return Position;
        var area = target.WorkingArea;
        var margin = (int)(24 * target.Scaling);
        return new PixelPoint(area.Right - (int)(Width * target.Scaling) - margin, area.Y + margin);
    }
}
