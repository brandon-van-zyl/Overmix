namespace Overmix.Core.Shortcuts;

[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Ctrl = 1,
    Alt = 2,
    Shift = 4,
    Win = 8,
}

/// <summary>A keyboard combination that works mid-game without leaving the game, such as Ctrl+Alt+M.</summary>
public readonly record struct Shortcut
{
    public Shortcut(ShortcutModifiers modifiers, string key)
    {
        if (!KeyNames.TryGetCanonical(key, out var canonical))
            throw new ArgumentException($"'{key}' isn't a key a Shortcut can use.", nameof(key));
        Modifiers = modifiers;
        Key = canonical;
    }

    public ShortcutModifiers Modifiers { get; }

    /// <summary>The key pressed with the modifiers, as named in <see cref="KeyNames"/>.</summary>
    public string Key { get; }

    public static Shortcut Parse(string text) =>
        TryParse(text, out var shortcut) ? shortcut : throw new FormatException($"'{text}' isn't a Shortcut like Ctrl+Alt+M.");

    public static bool TryParse(string? text, out Shortcut shortcut)
    {
        shortcut = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        var modifiers = ShortcutModifiers.None;
        foreach (var part in parts[..^1])
        {
            var modifier = part.ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => ShortcutModifiers.Ctrl,
                "ALT" => ShortcutModifiers.Alt,
                "SHIFT" => ShortcutModifiers.Shift,
                "WIN" or "WINDOWS" => ShortcutModifiers.Win,
                _ => ShortcutModifiers.None,
            };
            if (modifier == ShortcutModifiers.None) return false;
            modifiers |= modifier;
        }

        if (!KeyNames.TryGetCanonical(parts[^1], out var key)) return false;
        shortcut = new Shortcut(modifiers, key);
        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(ShortcutModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ShortcutModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ShortcutModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ShortcutModifiers.Win)) parts.Add("Win");
        parts.Add(Key ?? "");
        return string.Join('+', parts);
    }
}
