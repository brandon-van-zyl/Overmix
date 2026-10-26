using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Overmix.Core;
using Overmix.Core.Settings;

namespace Overmix.Views;

/// <summary>
/// The Overlay: a compact strip that pops up over the Game. It takes the mouse while it's open, and the
/// Popup Shortcut or Esc closes it and hands focus straight back to the Game. Drag it to move it.
/// </summary>
public sealed partial class OverlayWindow : Window
{
    private readonly IGameFocus? _gameFocus;
    private bool _placed;
    private bool _moved;

    public OverlayWindow() => InitializeComponent();

    public OverlayWindow(IGameFocus gameFocus, WindowSpot? spot)
        : this()
    {
        _gameFocus = gameFocus;
        Spot = spot;
        Strip.PointerPressed += OnStripPressed;
    }

    /// <summary>Where the user dragged the Overlay to, if they did.</summary>
    public WindowSpot? Spot { get; private set; }

    /// <summary>The Overlay was moved and closed; remember where it is.</summary>
    public event EventHandler? Moved;

    public void Toggle()
    {
        if (IsVisible) Dismiss();
        else Open();
    }

    public void Open()
    {
        _gameFocus?.Remember();

        // The first time, its size isn't known until it's shown, so show it invisibly and place it.
        if (!_placed) Opacity = 0;
        Show();
        if (!_placed)
        {
            Place();
            Opacity = 1;
            _placed = true;
        }

        Activate();
    }

    public void Dismiss()
    {
        if (!IsVisible) return;
        Hide();
        if (_moved)
        {
            _moved = false;
            Spot = new WindowSpot(Position.X, Position.Y);
            Moved?.Invoke(this, EventArgs.Empty);
        }

        _gameFocus?.Return();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != Key.Escape) return;
        Dismiss();
        e.Handled = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        // Alt+F4 only hides it, like Esc. The window lives as long as Overmix does.
        if (e.CloseReason == WindowCloseReason.ApplicationShutdown) return;
        e.Cancel = true;
        Dismiss();
    }

    private void OnStripPressed(object? sender, PointerPressedEventArgs e)
    {
        // Dials and buttons handle their own presses; anywhere else on the strip drags it.
        if (e.Handled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _moved = true;
        BeginMoveDrag(e);
    }

    /// <summary>Centred on the main screen, unless the user has moved it somewhere that's still on a screen.</summary>
    private void Place()
    {
        if (Spot is { } spot && Screens.All.Any(screen => screen.Bounds.Contains(new PixelPoint(spot.X + 40, spot.Y + 40))))
        {
            Position = new PixelPoint(spot.X, spot.Y);
            return;
        }

        if ((Screens.Primary ?? Screens.All.FirstOrDefault()) is not { } screen) return;
        var size = PixelSize.FromSize(Bounds.Size, screen.Scaling);
        var area = screen.Bounds;
        Position = new PixelPoint(area.X + (area.Width - size.Width) / 2, area.Y + (area.Height - size.Height) / 2);
    }
}
