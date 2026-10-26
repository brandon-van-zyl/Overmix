using Overmix.Core.Media;

namespace Overmix.Fakes;

/// <summary>A pretend player working through a playlist.</summary>
public sealed class FakeNowPlaying(params Track[] playlist) : INowPlaying
{
    private int _index;

    public Track? Track => playlist.Length == 0 ? null : playlist[_index];

    public bool IsPlaying { get; private set; }

    public event EventHandler? Changed;

    public void PlayPause()
    {
        if (Track is null) return;
        IsPlaying = !IsPlaying;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Next() => Skip(1);

    public void Previous() => Skip(-1);

    private void Skip(int by)
    {
        if (playlist.Length == 0) return;
        _index = (_index + by + playlist.Length) % playlist.Length;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
