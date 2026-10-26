using Overmix.Core.Shortcuts;

namespace Overmix.Core.Sound;

/// <summary>A program on your list whose sound you control, such as Discord or Spotify.</summary>
public sealed record App(ProgramName Program, string Name)
{
    /// <summary>Where the program lives, once known, so its icon shows even while it isn't running.</summary>
    public string? Path { get; init; }

    /// <summary>The Shortcut that turns this App up, if it has one.</summary>
    public Shortcut? VolumeUp { get; init; }

    /// <summary>The Shortcut that turns this App down, if it has one.</summary>
    public Shortcut? VolumeDown { get; init; }
}
