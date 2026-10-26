namespace Overmix.Core.Shortcuts;

/// <summary>Listens for Shortcuts anywhere, even while a game has focus. Raises events on the UI thread.</summary>
public interface IShortcutKeys
{
    /// <summary>Starts listening for a Shortcut. Returns false if it can't, usually because another app uses it.</summary>
    bool Register(Shortcut shortcut);

    void Unregister(Shortcut shortcut);

    event EventHandler<Shortcut>? Pressed;
}
