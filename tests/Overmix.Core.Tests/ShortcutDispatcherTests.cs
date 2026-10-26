using Overmix.Core.Shortcuts;
using Overmix.Core.Sound;
using Overmix.Core.Voice;
using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class ShortcutDispatcherTests
{
    private readonly FakePc _pc = new();
    private readonly AppList _apps = new(
    [
        new App(new("Discord"), "Discord")
        {
            VolumeUp = Shortcut.Parse("Ctrl+Alt+Up"),
            VolumeDown = Shortcut.Parse("Ctrl+Alt+Down"),
        },
    ]);

    [Fact]
    public void The_Mic_Mute_Shortcut_toggles_Mic_Mute()
    {
        _pc.Microphones.Plug("headset-mic");
        var (_, micMute) = CreateDispatcher();

        _pc.ShortcutKeys.Press("Ctrl+Alt+M");
        Assert.True(micMute.IsOn);

        _pc.ShortcutKeys.Press("Ctrl+Alt+M");
        Assert.False(micMute.IsOn);
    }

    [Fact]
    public void An_Apps_volume_Shortcuts_turn_its_Dial_by_five()
    {
        _pc.Sound.Start("Discord", 40);
        CreateDispatcher();

        _pc.ShortcutKeys.Press("Ctrl+Alt+Up");
        Assert.Equal(new Volume(45), _pc.Sound.GetVolume(new("Discord")));

        _pc.ShortcutKeys.Press("Ctrl+Alt+Down");
        _pc.ShortcutKeys.Press("Ctrl+Alt+Down");
        Assert.Equal(new Volume(35), _pc.Sound.GetVolume(new("Discord")));
    }

    [Fact]
    public void The_Popup_Shortcut_says_it_was_pressed()
    {
        var (dispatcher, _) = CreateDispatcher();
        var presses = 0;
        dispatcher.PopupPressed += (_, _) => presses++;

        _pc.ShortcutKeys.Press("Ctrl+Alt+O");

        Assert.Equal(1, presses);
    }

    [Fact]
    public void Shortcuts_another_app_uses_are_reported_unavailable()
    {
        _pc.ShortcutKeys.TakenByOtherApps.Add(Shortcut.Parse("Ctrl+Alt+M"));

        var (dispatcher, _) = CreateDispatcher();

        Assert.Equal([Shortcut.Parse("Ctrl+Alt+M")], dispatcher.Unavailable);
    }

    [Fact]
    public void Changing_a_Shortcut_swaps_which_keys_are_registered()
    {
        var (dispatcher, _) = CreateDispatcher();

        dispatcher.SetPopupShortcut(Shortcut.Parse("F10"));

        Assert.Contains(Shortcut.Parse("F10"), _pc.ShortcutKeys.Registered);
        Assert.DoesNotContain(Shortcut.Parse("Ctrl+Alt+O"), _pc.ShortcutKeys.Registered);
    }

    [Fact]
    public void Removing_an_App_releases_its_Shortcuts()
    {
        CreateDispatcher();

        _apps.Remove(new("Discord"));

        Assert.DoesNotContain(Shortcut.Parse("Ctrl+Alt+Up"), _pc.ShortcutKeys.Registered);
    }

    private (ShortcutDispatcher Dispatcher, MicMute MicMute) CreateDispatcher()
    {
        var mixer = new Mixer(_pc.Sound, _apps, new GameTracker(_pc.MainScreen));
        var micMute = new MicMute(_pc.Microphones);
        var dispatcher = new ShortcutDispatcher(
            _pc.ShortcutKeys, _apps, mixer, micMute, Shortcut.Parse("Ctrl+Alt+O"), Shortcut.Parse("Ctrl+Alt+M"));
        return (dispatcher, micMute);
    }
}
