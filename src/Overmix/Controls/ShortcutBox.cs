using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Overmix.Core.Shortcuts;

namespace Overmix.Controls;

/// <summary>
/// Shows a Shortcut and records a new one: click it, then press the keys. Backspace or Delete clears it,
/// and Esc keeps the old one.
/// </summary>
public sealed class ShortcutBox : Button
{
    public static readonly StyledProperty<Shortcut?> ShortcutProperty = AvaloniaProperty.Register<ShortcutBox, Shortcut?>(
        nameof(Shortcut), defaultBindingMode: BindingMode.TwoWay);

    private bool _recording;

    public ShortcutBox()
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        ShowCurrent();
    }

    public Shortcut? Shortcut
    {
        get => GetValue(ShortcutProperty);
        set => SetValue(ShortcutProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(Button);

    protected override void OnClick()
    {
        base.OnClick();
        _recording = true;
        Content = "Press keys…";
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!_recording)
        {
            base.OnKeyDown(e);
            return;
        }

        e.Handled = true;
        switch (e.Key)
        {
            case Key.Escape:
                Stop();
                return;
            case Key.Back or Key.Delete when e.KeyModifiers == KeyModifiers.None:
                Shortcut = null;
                Stop();
                return;
        }

        // Wait for a real key; the modifiers alone aren't a Shortcut.
        if (AvaloniaKeys.NameOf(e.Key) is not { } key) return;
        Shortcut = new Shortcut(AvaloniaKeys.ToModifiers(e.KeyModifiers), key);
        Stop();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (_recording) Stop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShortcutProperty && !_recording) ShowCurrent();
    }

    private void Stop()
    {
        _recording = false;
        ShowCurrent();
    }

    private void ShowCurrent() => Content = Shortcut?.ToString() ?? "Not set";
}
