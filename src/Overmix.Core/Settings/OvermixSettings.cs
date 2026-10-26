namespace Overmix.Core.Settings;

/// <summary>Everything Overmix remembers between runs.</summary>
public sealed record OvermixSettings
{
    public IReadOnlyList<AppSettings> Apps { get; init; } = [];

    /// <summary>Apps you removed. They don't come back by themselves, and keep their setup if you bring them back.</summary>
    public IReadOnlyList<AppSettings> RemovedApps { get; init; } = [];

    public string? PopupShortcut { get; init; } = "Ctrl+Alt+O";

    public string? MicMuteShortcut { get; init; } = "Ctrl+Alt+M";

    public string? OutputFirst { get; init; }

    public string? OutputSecond { get; init; }

    public bool PanelOpen { get; init; } = true;

    public bool MuteChime { get; init; } = true;

    public bool MuteBorder { get; init; } = true;

    public WindowSpot? PanelSpot { get; init; }

    public WindowSpot? OverlaySpot { get; init; }
}

public sealed record AppSettings(
    string Program, string Name, string? VolumeUp = null, string? VolumeDown = null, string? Path = null);

/// <summary>Where a window sits on screen, in screen pixels.</summary>
public sealed record WindowSpot(int X, int Y);
