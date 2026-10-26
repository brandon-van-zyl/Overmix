using Overmix.Core;
using Overmix.Core.Voice;

namespace Overmix.Fakes;

/// <summary>Pretend microphones that can be plugged in, unplugged, and muted from outside Overmix.</summary>
public sealed class FakeMicrophones : IMicrophones
{
    private readonly Dictionary<DeviceId, bool> _muted = [];

    public IReadOnlyCollection<DeviceId> All => _muted.Keys.ToList();

    public event EventHandler<DeviceId>? Added;

    public event EventHandler<DeviceId>? Removed;

    public event EventHandler<DeviceId>? MuteChanged;

    public bool IsMuted(DeviceId mic) => _muted.GetValueOrDefault(mic);

    public bool IsMuted(string mic) => IsMuted(new DeviceId(mic));

    public void SetMuted(DeviceId mic, bool muted)
    {
        if (!_muted.TryGetValue(mic, out var was) || was == muted) return;
        _muted[mic] = muted;
        MuteChanged?.Invoke(this, mic);
    }

    public void Plug(string mic, bool muted = false)
    {
        var id = new DeviceId(mic);
        _muted[id] = muted;
        Added?.Invoke(this, id);
    }

    public void Unplug(string mic)
    {
        var id = new DeviceId(mic);
        if (_muted.Remove(id)) Removed?.Invoke(this, id);
    }

    /// <summary>Something other than Overmix mutes or unmutes a mic, such as Windows' sound settings.</summary>
    public void MuteFromOutside(string mic, bool muted) => SetMuted(new DeviceId(mic), muted);
}
