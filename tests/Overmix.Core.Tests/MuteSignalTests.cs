using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class MuteSignalTests
{
    private readonly FakePc _pc = FakePc.GamingEvening();
    private readonly InMemorySettingsStore _store = new(FakePc.SampleSettings());

    [Fact]
    public void The_Mute_Chime_plays_on_muting_and_unmuting()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        core.MicMute.Toggle();
        core.MicMute.Toggle();

        Assert.Equal([true, false], _pc.MuteChime.Played);
    }

    [Fact]
    public void The_Mute_Chime_stays_quiet_when_switched_off()
    {
        var core = new OvermixCore(_pc.Platform, _store) { MuteChimeOn = false };

        core.MicMute.Toggle();

        Assert.Empty(_pc.MuteChime.Played);
    }

    [Fact]
    public void The_Mute_Border_shows_only_while_Mic_Mute_is_on()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        core.MicMute.Toggle();
        Assert.True(_pc.MuteBorder.IsShown);

        core.MicMute.Toggle();
        Assert.False(_pc.MuteBorder.IsShown);
    }

    [Fact]
    public void The_Mute_Border_shows_at_start_if_the_mic_is_still_muted()
    {
        _pc.Microphones.MuteFromOutside("headset-mic", true);
        _pc.Microphones.MuteFromOutside("webcam-mic", true);

        _ = new OvermixCore(_pc.Platform, _store);

        Assert.True(_pc.MuteBorder.IsShown);
    }

    [Fact]
    public void Switching_the_Mute_Border_off_hides_it_straight_away()
    {
        var core = new OvermixCore(_pc.Platform, _store);
        core.MicMute.Toggle();

        core.MuteBorderOn = false;

        Assert.False(_pc.MuteBorder.IsShown);
        Assert.False(new OvermixCore(_pc.Platform, _store).MuteBorderOn);
    }
}
