using Overmix.Core;
using Overmix.Core.Media;
using Overmix.Core.Settings;

namespace Overmix.Fakes;

/// <summary>A pretend gaming PC made of fakes.</summary>
public sealed class FakePc
{
    public FakePc(params Track[] playlist) => NowPlaying = new FakeNowPlaying(playlist);

    public FakeProgramSound Sound { get; } = new();

    public FakeMainScreen MainScreen { get; } = new();

    public FakeMicrophones Microphones { get; } = new();

    public FakeSoundOutputs Outputs { get; } = new();

    public FakeNowPlaying NowPlaying { get; }

    public FakeShortcutKeys ShortcutKeys { get; } = new();

    public FakeStartWithWindows StartWithWindows { get; } = new();

    public FakeGameFocus GameFocus { get; } = new();

    public FakeProgramIcons Icons { get; } = new();

    public Platform Platform =>
        new(Sound, MainScreen, Microphones, Outputs, NowPlaying, ShortcutKeys, StartWithWindows, GameFocus, Icons);

    /// <summary>VALORANT running borderless, Discord and Spotify playing, a headset and speakers plugged in.</summary>
    public static FakePc GamingEvening()
    {
        var pc = new FakePc(new Track("Neon Skyline", "The Respawns"), new Track("Clutch Factor", "Low Ping"));
        pc.Sound.Start("VALORANT-Win64-Shipping", 80, "VALORANT");
        pc.Sound.Start("Discord", 45);
        pc.Sound.Start("Spotify", 30);
        pc.MainScreen.Fill("VALORANT-Win64-Shipping", "VALORANT");
        pc.Microphones.Plug("headset-mic");
        pc.Microphones.Plug("webcam-mic");
        pc.Outputs.Plug("headset", "Headset");
        pc.Outputs.Plug("speakers", "Speakers");
        pc.NowPlaying.PlayPause();
        pc.Icons.Give("VALORANT-Win64-Shipping", 255, 70, 85);
        pc.Icons.Give("Discord", 88, 101, 242);
        pc.Icons.Give("Spotify", 30, 215, 96);
        return pc;
    }

    /// <summary>Discord, Spotify and Chrome as Apps, with headset and speakers as the Output Switch pair.</summary>
    public static OvermixSettings SampleSettings() => new()
    {
        Apps =
        [
            new AppSettings("Discord", "Discord", "Ctrl+Alt+Up", "Ctrl+Alt+Down"),
            new AppSettings("Spotify", "Spotify"),
            new AppSettings("chrome", "Chrome"),
        ],
        OutputFirst = "headset",
        OutputSecond = "speakers",
    };
}
