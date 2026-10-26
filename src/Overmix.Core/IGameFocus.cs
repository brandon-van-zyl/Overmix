namespace Overmix.Core;

/// <summary>Hands focus back to the Game when the Overlay closes.</summary>
public interface IGameFocus
{
    /// <summary>Notes the window that has focus, just before the Overlay takes it.</summary>
    void Remember();

    /// <summary>Gives focus back to the window noted by <see cref="Remember"/>.</summary>
    void Return();
}
