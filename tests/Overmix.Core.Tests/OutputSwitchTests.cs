using Overmix.Core.Sound;
using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class OutputSwitchTests
{
    private static readonly OutputPair HeadsetAndSpeakers = new(new DeviceId("headset"), new DeviceId("speakers"));

    private readonly FakeSoundOutputs _outputs = new();

    public OutputSwitchTests()
    {
        _outputs.Plug("headset", "Headset");
        _outputs.Plug("speakers", "Speakers");
        _outputs.Plug("monitor", "Monitor");
    }

    [Fact]
    public void Flip_goes_from_headset_to_speakers_and_back()
    {
        var outputSwitch = new OutputSwitch(_outputs, HeadsetAndSpeakers);

        outputSwitch.Flip();
        Assert.Equal("Speakers", outputSwitch.Current?.Name);

        outputSwitch.Flip();
        Assert.Equal("Headset", outputSwitch.Current?.Name);
    }

    [Fact]
    public void Flip_from_an_output_outside_the_pair_goes_to_the_first_of_the_pair()
    {
        _outputs.MakeCurrent(new DeviceId("monitor"));
        var outputSwitch = new OutputSwitch(_outputs, HeadsetAndSpeakers);

        outputSwitch.Flip();

        Assert.Equal("Headset", outputSwitch.Current?.Name);
    }

    [Fact]
    public void Flip_does_nothing_when_the_other_output_is_unplugged()
    {
        var outputSwitch = new OutputSwitch(_outputs, HeadsetAndSpeakers);
        _outputs.Unplug("speakers");

        Assert.False(outputSwitch.Flip());
        Assert.Equal("Headset", outputSwitch.Current?.Name);
    }

    [Fact]
    public void Flip_does_nothing_until_a_pair_is_chosen()
    {
        var outputSwitch = new OutputSwitch(_outputs, pair: null);

        Assert.False(outputSwitch.Flip());
        Assert.Null(outputSwitch.Other);
    }
}
