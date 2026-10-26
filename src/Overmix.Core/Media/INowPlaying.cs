namespace Overmix.Core.Media;

/// <summary>A song or video playing in any app.</summary>
public sealed record Track(string Title, string Artist);

/// <summary>
/// Now Playing: whatever song or video is playing in any app, with play/pause, next and previous.
/// Raises events on the UI thread.
/// </summary>
public interface INowPlaying
{
    Track? Track { get; }

    bool IsPlaying { get; }

    void PlayPause();

    void Next();

    void Previous();

    /// <summary>The track, or whether it's playing, changed.</summary>
    event EventHandler? Changed;
}
