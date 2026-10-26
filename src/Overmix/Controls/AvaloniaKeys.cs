using System.Globalization;
using Avalonia.Input;
using Overmix.Core.Shortcuts;

namespace Overmix.Controls;

/// <summary>Translates Avalonia's keys into the key names Shortcuts use.</summary>
internal static class AvaloniaKeys
{
    public static string? NameOf(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => (key - Key.D0).ToString(CultureInfo.InvariantCulture),
        >= Key.NumPad0 and <= Key.NumPad9 => $"NumPad{key - Key.NumPad0}",
        >= Key.F1 and <= Key.F24 => $"F{key - Key.F1 + 1}",
        Key.Space => "Space",
        Key.Enter => "Enter",
        Key.Tab => "Tab",
        Key.Back => "Backspace",
        Key.Insert => "Insert",
        Key.Delete => "Delete",
        Key.Home => "Home",
        Key.End => "End",
        Key.PageUp => "PageUp",
        Key.PageDown => "PageDown",
        Key.Up => "Up",
        Key.Down => "Down",
        Key.Left => "Left",
        Key.Right => "Right",
        Key.Pause => "Pause",
        Key.Scroll => "ScrollLock",
        Key.Add => "NumPadAdd",
        Key.Subtract => "NumPadSubtract",
        Key.Multiply => "NumPadMultiply",
        Key.Divide => "NumPadDivide",
        Key.Decimal => "NumPadDecimal",
        Key.OemTilde => "`",
        Key.OemMinus => "-",
        Key.OemPlus => "=",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemPipe => "\\",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemQuestion => "/",
        _ => null,
    };

    public static ShortcutModifiers ToModifiers(KeyModifiers modifiers)
    {
        var result = ShortcutModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= ShortcutModifiers.Ctrl;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= ShortcutModifiers.Alt;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= ShortcutModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Meta)) result |= ShortcutModifiers.Win;
        return result;
    }
}
