namespace Overmix.Core.Voice;

/// <summary>
/// Mic Mute: every microphone on the PC muted in Windows itself. It never changes what you hear.
/// It always shows the real state, whatever changed it, and mutes a mic plugged in while it's on.
/// </summary>
public sealed class MicMute
{
    private readonly IMicrophones _mics;

    public MicMute(IMicrophones mics)
    {
        _mics = mics;
        mics.Added += (_, mic) =>
        {
            if (IsOn) _mics.SetMuted(mic, true);
            Recheck();
        };
        mics.Removed += (_, _) => Recheck();
        mics.MuteChanged += (_, _) => Recheck();
        IsOn = mics.All.Count > 0 && mics.All.All(mics.IsMuted);
    }

    public bool IsOn { get; private set; }

    public event EventHandler? Changed;

    public void Toggle() => Set(!IsOn);

    public void Set(bool on)
    {
        if (_mics.All.Count == 0)
        {
            // Nothing to mute yet, but a mic plugged in later should follow the choice.
            Update(on);
            return;
        }

        foreach (var mic in _mics.All.ToList()) _mics.SetMuted(mic, on);
        Recheck();
    }

    private void Recheck()
    {
        // With no mics left there's nothing to judge by, so keep the state: a mic plugged back in gets muted again.
        if (_mics.All.Count > 0) Update(_mics.All.All(_mics.IsMuted));
    }

    private void Update(bool on)
    {
        if (on == IsOn) return;
        IsOn = on;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
