using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Overmix.Controls;

/// <summary>
/// A volume dial: a 270° ring that fills with the volume, the program's icon in the middle and the
/// number in the gap at the bottom. Scroll it, drag it up and down, or use the arrow keys.
/// It draws only when something about it changes.
/// </summary>
public sealed class DialControl : Control
{
    public static readonly StyledProperty<int> ValueProperty = AvaloniaProperty.Register<DialControl, int>(
        nameof(Value), defaultBindingMode: BindingMode.TwoWay, coerce: (_, value) => Math.Clamp(value, 0, 100));

    public static readonly StyledProperty<bool> IsConnectedProperty =
        AvaloniaProperty.Register<DialControl, bool>(nameof(IsConnected));

    public static readonly StyledProperty<IImage?> IconProperty = AvaloniaProperty.Register<DialControl, IImage?>(nameof(Icon));

    public static readonly StyledProperty<string?> MonogramProperty =
        AvaloniaProperty.Register<DialControl, string?>(nameof(Monogram));

    public static readonly StyledProperty<IBrush?> AccentProperty = AvaloniaProperty.Register<DialControl, IBrush?>(nameof(Accent));

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<DialControl, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<IBrush?> TextBrushProperty =
        AvaloniaProperty.Register<DialControl, IBrush?>(nameof(TextBrush));

    /// <summary>How far one scroll notch or arrow key turns the dial, matching the volume Shortcuts.</summary>
    private const int Step = 5;

    private const double StartAngle = 135;
    private const double Sweep = 270;

    private double? _dragStartY;
    private int _dragStartValue;

    static DialControl()
    {
        AffectsRender<DialControl>(
            ValueProperty, IsConnectedProperty, IconProperty, MonogramProperty, AccentProperty, TrackBrushProperty, TextBrushProperty);
        FocusableProperty.OverrideDefaultValue<DialControl>(true);
        CursorProperty.OverrideDefaultValue<DialControl>(new Cursor(StandardCursorType.SizeNorthSouth));
    }

    public DialControl() => RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsConnected
    {
        get => GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The letter to show when there's no icon.</summary>
    public string? Monogram
    {
        get => GetValue(MonogramProperty);
        set => SetValue(MonogramProperty, value);
    }

    public IBrush? Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0) return;

        var thickness = Math.Max(3, size * 0.07);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = size / 2 - thickness / 2 - 1;
        var typeface = new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.SemiBold);

        DrawArc(context, center, radius, Sweep, new Pen(TrackBrush, thickness, lineCap: PenLineCap.Round));
        if (IsConnected && Value > 0)
            DrawArc(context, center, radius, Sweep * Value / 100, new Pen(Accent, thickness, lineCap: PenLineCap.Round));

        // The icon, or a letter, in the middle; faded while the program isn't running.
        var art = Math.Round((radius - thickness) * 1.15);
        var artBox = new Rect(center.X - art / 2, center.Y - art / 2 - size * 0.03, art, art);
        using (context.PushOpacity(IsConnected ? 1 : 0.35))
        {
            if (Icon is { } icon)
            {
                context.DrawImage(icon, artBox);
            }
            else if (!string.IsNullOrEmpty(Monogram))
            {
                var letter = new FormattedText(
                    Monogram, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, art * 0.6, TextBrush);
                context.DrawText(letter, new Point(artBox.Center.X - letter.Width / 2, artBox.Center.Y - letter.Height / 2));
            }
        }

        // The number sits in the ring's gap at the bottom.
        var number = new FormattedText(
            IsConnected ? Value.ToString(CultureInfo.CurrentCulture) : "–",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Math.Max(9, size * 0.15), TextBrush);
        context.DrawText(number, new Point(center.X - number.Width / 2, center.Y + radius - number.Height / 2));
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!IsConnected || e.Delta.Y == 0) return;
        Value += Math.Sign(e.Delta.Y) * Step;
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsConnected || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragStartY = e.GetPosition(this).Y;
        _dragStartValue = Value;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragStartY is not { } startY) return;

        // Two pixels up is one point louder.
        Value = _dragStartValue + (int)Math.Round((startY - e.GetPosition(this).Y) / 2);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragStartY is null) return;
        _dragStartY = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsConnected) return;
        var by = e.Key switch
        {
            Key.Up or Key.Right => Step,
            Key.Down or Key.Left => -Step,
            Key.PageUp => 20,
            Key.PageDown => -20,
            _ => 0,
        };
        if (by == 0) return;
        Value += by;
        e.Handled = true;
    }

    private static void DrawArc(DrawingContext context, Point center, double radius, double sweep, IPen pen)
    {
        if (sweep <= 0) return;
        var geometry = new StreamGeometry();
        using (var figure = geometry.Open())
        {
            figure.BeginFigure(PointAt(center, radius, StartAngle), isFilled: false);
            figure.ArcTo(
                PointAt(center, radius, StartAngle + sweep), new Size(radius, radius), 0, isLargeArc: sweep > 180, SweepDirection.Clockwise);
            figure.EndFigure(isClosed: false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private static Point PointAt(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }
}
