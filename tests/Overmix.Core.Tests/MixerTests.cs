using Overmix.Core.Sound;
using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class MixerTests
{
    private readonly FakePc _pc = new();
    private readonly AppList _apps = new([new App(new("Discord"), "Discord"), new App(new("Spotify"), "Spotify")]);

    [Fact]
    public void The_Game_Dial_comes_first_then_one_Dial_per_App()
    {
        var mixer = CreateMixer();

        DialTarget[] expected = [DialTarget.Game, DialTarget.For(new("Discord")), DialTarget.For(new("Spotify"))];
        Assert.Equal(expected, mixer.Dials.Select(dial => dial.Target));
    }

    [Fact]
    public void An_App_Dial_is_greyed_out_until_its_program_uses_sound()
    {
        var mixer = CreateMixer();
        Assert.False(DialFor(mixer, "Discord").IsConnected);

        _pc.Sound.Start("Discord", 45);

        Assert.Equal(new Volume(45), DialFor(mixer, "Discord").Volume);
    }

    [Fact]
    public void An_App_Dial_greys_out_again_when_its_program_quits()
    {
        _pc.Sound.Start("Discord", 45);
        var mixer = CreateMixer();

        _pc.Sound.Stop("Discord");

        Assert.False(DialFor(mixer, "Discord").IsConnected);
    }

    [Fact]
    public void Programs_not_on_the_list_get_no_Dial()
    {
        var mixer = CreateMixer();

        _pc.Sound.Start("chrome");

        Assert.Equal(3, mixer.Dials.Count);
    }

    [Fact]
    public void Turning_a_Dial_sets_its_programs_volume()
    {
        _pc.Sound.Start("Spotify", 30);
        var mixer = CreateMixer();

        mixer.SetVolume(DialTarget.For(new("Spotify")), new Volume(60));

        Assert.Equal(new Volume(60), _pc.Sound.GetVolume(new("Spotify")));
    }

    [Fact]
    public void A_volume_changed_outside_Overmix_shows_on_the_Dial()
    {
        _pc.Sound.Start("Spotify", 30);
        var mixer = CreateMixer();
        var changes = 0;
        mixer.Changed += (_, _) => changes++;

        _pc.Sound.SetVolume(new("Spotify"), new Volume(10));

        Assert.Equal(new Volume(10), DialFor(mixer, "Spotify").Volume);
        Assert.Equal(1, changes);
    }

    [Theory]
    [InlineData(98, 5, 100)]
    [InlineData(3, -5, 0)]
    [InlineData(40, 5, 45)]
    public void Nudging_stays_between_silent_and_full(int start, int by, int expected)
    {
        _pc.Sound.Start("Discord", start);
        var mixer = CreateMixer();

        mixer.Nudge(DialTarget.For(new("Discord")), by);

        Assert.Equal(new Volume(expected), _pc.Sound.GetVolume(new("Discord")));
    }

    [Fact]
    public void The_Game_Dial_controls_the_Game()
    {
        _pc.Sound.Start("VALORANT-Win64-Shipping", 80);
        var mixer = CreateMixer();

        _pc.MainScreen.Fill("VALORANT-Win64-Shipping", "VALORANT");
        mixer.SetVolume(DialTarget.Game, new Volume(50));

        Assert.Equal("VALORANT", mixer.Dials[0].Name);
        Assert.Equal(new Volume(50), _pc.Sound.GetVolume(new("VALORANT-Win64-Shipping")));
    }

    [Fact]
    public void The_Games_own_App_Dial_waits_while_it_is_the_Game()
    {
        _apps.Add(new App(new("cs2"), "Counter-Strike 2"));
        var mixer = CreateMixer();

        _pc.MainScreen.Fill("cs2", "Counter-Strike 2");
        Assert.Single(mixer.Dials, dial => dial.Program == new ProgramName("cs2"));

        _pc.MainScreen.Quit("cs2");
        Assert.Contains(mixer.Dials, dial => dial.Target == DialTarget.For(new("cs2")));
    }

    [Fact]
    public void The_Game_Dial_is_greyed_out_without_a_Game()
    {
        var mixer = CreateMixer();

        Assert.False(mixer.Dials[0].IsConnected);
        Assert.Equal("", mixer.Dials[0].Name);
    }

    private Mixer CreateMixer() => new(_pc.Sound, _apps, new GameTracker(_pc.MainScreen));

    private static Dial DialFor(Mixer mixer, string program) =>
        mixer.Dials.Single(dial => dial.Target == DialTarget.For(new ProgramName(program)));
}
