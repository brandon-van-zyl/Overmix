using System.Diagnostics;
using System.Runtime.InteropServices;
using Overmix.Core.Media;
using Windows.Media.Control;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Now Playing from Windows' media controls: the same source as the media flyout next to the volume
/// slider. Event-driven, so it costs nothing while the track doesn't change.
/// </summary>
internal sealed class WindowsNowPlaying : INowPlaying, IDisposable
{
    private readonly SynchronizationContext _ui;
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;

    public WindowsNowPlaying(SynchronizationContext ui)
    {
        _ui = ui;
        _ = StartAsync();
    }

    public Track? Track { get; private set; }

    public bool IsPlaying { get; private set; }

    public event EventHandler? Changed;

    public void PlayPause() => _ = _session?.TryTogglePlayPauseAsync();

    public void Next() => _ = _session?.TrySkipNextAsync();

    public void Previous() => _ = _session?.TrySkipPreviousAsync();

    public void Dispose()
    {
        if (_manager is not null) _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        Follow(null);
    }

    private async Task StartAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += OnCurrentSessionChanged;
            Follow(_manager.GetCurrentSession());
        }
        catch (Exception e) when (e is COMException or InvalidOperationException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"Overmix now playing: unavailable: {e.Message}");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager manager, CurrentSessionChangedEventArgs args) =>
        _ui.Post(_ => Follow(manager.GetCurrentSession()), null);

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession session, MediaPropertiesChangedEventArgs args) =>
        _ui.Post(_ => _ = RefreshAsync(), null);

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession session, PlaybackInfoChangedEventArgs args) =>
        _ui.Post(_ => _ = RefreshAsync(), null);

    private void Follow(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        _session = session;
        if (session is not null)
        {
            session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        }

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var session = _session;
        Track? track = null;
        var playing = false;
        if (session is not null)
        {
            try
            {
                var media = await session.TryGetMediaPropertiesAsync();
                if (session != _session) return;
                if (!string.IsNullOrWhiteSpace(media?.Title)) track = new Track(media.Title, media.Artist ?? "");
                playing = session.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            }
            catch (Exception e) when (e is COMException or InvalidOperationException)
            {
                // The player closed while we asked.
            }
        }

        if (track == Track && playing == IsPlaying) return;
        Track = track;
        IsPlaying = playing;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
