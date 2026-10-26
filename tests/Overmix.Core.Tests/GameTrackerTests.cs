using Overmix.Core.Sound;
using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class GameTrackerTests
{
    private readonly FakeMainScreen _screen = new();
    private readonly AppList _apps = new([new App(new("chrome"), "Chrome")]);

    [Fact]
    public void The_Game_is_the_most_recent_program_to_fill_the_main_screen()
    {
        var tracker = new GameTracker(_screen, _apps);

        _screen.Fill("VALORANT-Win64-Shipping", "VALORANT");
        _screen.Fill("cs2", "Counter-Strike 2");

        Assert.Equal(new Game(new("cs2"), "Counter-Strike 2"), tracker.Current);
    }

    [Fact]
    public void An_App_filling_the_screen_never_becomes_the_Game()
    {
        var tracker = new GameTracker(_screen, _apps);

        _screen.Fill("VALORANT-Win64-Shipping", "VALORANT");
        _screen.Fill("chrome", "YouTube in full screen");

        Assert.Equal("VALORANT", tracker.Current?.Name);
    }

    [Fact]
    public void A_game_already_filling_the_screen_at_start_is_the_Game()
    {
        _screen.Fill("cs2", "Counter-Strike 2");

        var tracker = new GameTracker(_screen, _apps);

        Assert.Equal("Counter-Strike 2", tracker.Current?.Name);
    }

    [Fact]
    public void There_is_no_Game_once_it_quits()
    {
        var tracker = new GameTracker(_screen, _apps);
        _screen.Fill("cs2", "Counter-Strike 2");

        _screen.Quit("cs2");

        Assert.Null(tracker.Current);
    }

    [Fact]
    public void Another_program_quitting_leaves_the_Game_alone()
    {
        var tracker = new GameTracker(_screen, _apps);
        _screen.Fill("cs2", "Counter-Strike 2");

        _screen.Quit("notepad");

        Assert.Equal("Counter-Strike 2", tracker.Current?.Name);
    }

    [Fact]
    public void Adding_the_Game_as_an_App_takes_it_off_the_Game_Dial()
    {
        var tracker = new GameTracker(_screen, _apps);
        _screen.Fill("cs2", "Counter-Strike 2");

        _apps.Add(new App(new("cs2"), "CS2"));

        Assert.Null(tracker.Current);
    }
}
