using System.Globalization;

namespace Overmix.Core.Shortcuts;

/// <summary>The names of the keys a Shortcut can end in, such as "M", "F9", "NumPad1" or "`".</summary>
public static class KeyNames
{
    private static readonly HashSet<string> Known = new(Build(), StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> All => Known;

    /// <summary>Finds the standard spelling of a key name, ignoring case.</summary>
    public static bool TryGetCanonical(string? name, out string canonical)
    {
        if (name is not null && Known.TryGetValue(name.Trim(), out var known))
        {
            canonical = known;
            return true;
        }

        canonical = "";
        return false;
    }

    private static IEnumerable<string> Build()
    {
        for (var letter = 'A'; letter <= 'Z'; letter++) yield return letter.ToString();
        for (var digit = 0; digit <= 9; digit++) yield return digit.ToString(CultureInfo.InvariantCulture);
        for (var function = 1; function <= 24; function++) yield return $"F{function}";
        for (var digit = 0; digit <= 9; digit++) yield return $"NumPad{digit}";

        string[] named =
        [
            "Space", "Enter", "Tab", "Backspace", "Insert", "Delete", "Home", "End", "PageUp", "PageDown",
            "Up", "Down", "Left", "Right", "Pause", "ScrollLock",
            "NumPadAdd", "NumPadSubtract", "NumPadMultiply", "NumPadDivide", "NumPadDecimal",
            "`", "-", "=", "[", "]", "\\", ";", "'", ",", ".", "/",
        ];
        foreach (var name in named) yield return name;
    }
}
