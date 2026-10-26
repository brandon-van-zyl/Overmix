using Overmix.Core.Voice;
using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class MicMuteTests
{
    private readonly FakeMicrophones _mics = new();

    public MicMuteTests()
    {
        _mics.Plug("headset-mic");
        _mics.Plug("webcam-mic");
    }

    [Fact]
    public void Mic_Mute_mutes_every_microphone()
    {
        var micMute = new MicMute(_mics);

        micMute.Toggle();

        Assert.True(micMute.IsOn);
        Assert.True(_mics.IsMuted("headset-mic"));
        Assert.True(_mics.IsMuted("webcam-mic"));
    }

    [Fact]
    public void Turning_Mic_Mute_off_unmutes_every_microphone()
    {
        var micMute = new MicMute(_mics);
        micMute.Set(true);

        micMute.Set(false);

        Assert.False(micMute.IsOn);
        Assert.False(_mics.IsMuted("headset-mic"));
        Assert.False(_mics.IsMuted("webcam-mic"));
    }

    [Fact]
    public void A_mic_plugged_in_while_Mic_Mute_is_on_is_muted_straight_away()
    {
        var micMute = new MicMute(_mics);
        micMute.Set(true);

        _mics.Plug("usb-mic");

        Assert.True(_mics.IsMuted("usb-mic"));
        Assert.True(micMute.IsOn);
    }

    [Fact]
    public void Unmuting_a_mic_outside_Overmix_turns_Mic_Mute_off()
    {
        var micMute = new MicMute(_mics);
        micMute.Set(true);
        var changes = 0;
        micMute.Changed += (_, _) => changes++;

        _mics.MuteFromOutside("webcam-mic", false);

        Assert.False(micMute.IsOn);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Mic_Mute_starts_on_when_every_mic_is_already_muted()
    {
        _mics.MuteFromOutside("headset-mic", true);
        _mics.MuteFromOutside("webcam-mic", true);

        Assert.True(new MicMute(_mics).IsOn);
    }

    [Fact]
    public void Mic_Mute_stays_on_while_every_mic_is_unplugged()
    {
        var micMute = new MicMute(_mics);
        micMute.Set(true);

        _mics.Unplug("headset-mic");
        _mics.Unplug("webcam-mic");
        _mics.Plug("headset-mic");

        Assert.True(micMute.IsOn);
        Assert.True(_mics.IsMuted("headset-mic"));
    }
}
