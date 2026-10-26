namespace Overmix.Core.Voice;

/// <summary>The PC's microphones. Raises events on the UI thread.</summary>
public interface IMicrophones
{
    IReadOnlyCollection<DeviceId> All { get; }

    bool IsMuted(DeviceId mic);

    void SetMuted(DeviceId mic, bool muted);

    /// <summary>A microphone was plugged in.</summary>
    event EventHandler<DeviceId>? Added;

    /// <summary>A microphone was unplugged.</summary>
    event EventHandler<DeviceId>? Removed;

    /// <summary>A microphone was muted or unmuted, by Overmix or by anything else.</summary>
    event EventHandler<DeviceId>? MuteChanged;
}
