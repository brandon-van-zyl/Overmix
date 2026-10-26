using Overmix.Core.Shortcuts;

namespace Overmix.Fakes;

/// <summary>A pretend keyboard. Only Shortcuts Overmix registered reach it.</summary>
public sealed class FakeShortcutKeys : IShortcutKeys
{
    private readonly HashSet<Shortcut> _registered = [];

    /// <summary>Shortcuts another app already uses. Only the keyboard hook still catches them.</summary>
    public HashSet<Shortcut> TakenByOtherApps { get; } = [];

    public IReadOnlyCollection<Shortcut> Registered => _registered;

    public bool UseKeyboardHook { get; set; }

    public event EventHandler<Shortcut>? Pressed;

    public bool Register(Shortcut shortcut)
    {
        if (TakenByOtherApps.Contains(shortcut) && !UseKeyboardHook) return false;
        _registered.Add(shortcut);
        return true;
    }

    public void Unregister(Shortcut shortcut) => _registered.Remove(shortcut);

    /// <summary>The user presses a key combination.</summary>
    public void Press(string shortcut)
    {
        var pressed = Shortcut.Parse(shortcut);
        if (_registered.Contains(pressed)) Pressed?.Invoke(this, pressed);
    }
}
