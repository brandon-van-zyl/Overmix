using Overmix.Core.Shortcuts;
using Overmix.Core.Sound;
using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class OvermixCoreTests
{
    private readonly FakePc _pc = FakePc.GamingEvening();
    private readonly InMemorySettingsStore _store = new(FakePc.SampleSettings());

    [Fact]
    public void Apps_and_Shortcuts_come_back_from_settings()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        Assert.Equal(["Discord", "Spotify", "Chrome"], core.Apps.Items.Take(3).Select(app => app.Name));
        Assert.Equal(Shortcut.Parse("Ctrl+Alt+Up"), core.Apps.Items[0].VolumeUp);
        Assert.Equal(Shortcut.Parse("Ctrl+Alt+O"), core.Shortcuts.PopupShortcut);
        Assert.Equal("Speakers", core.OutputSwitch.Other?.Name);
    }

    [Fact]
    public void Adding_an_App_is_remembered()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        core.Apps.Add(new App(new("obs64"), "OBS"));

        Assert.Contains(_store.Saved.Apps, app => app is { Program: "obs64", Name: "OBS" });
    }

    [Fact]
    public void Programs_playing_sound_become_Apps_by_themselves()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        _pc.Sound.Start("obs64", name: "OBS Studio");

        Assert.Equal(["Discord", "Spotify", "Chrome", "VALORANT", "OBS Studio"], core.Apps.Items.Select(app => app.Name));
        Assert.Contains(_store.Saved.Apps, app => app.Program == "obs64");
    }

    [Fact]
    public void A_removed_App_doesnt_come_back_by_itself()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        core.RemoveApp(new("Spotify"));
        _pc.Sound.Stop("Spotify");
        _pc.Sound.Start("Spotify");

        Assert.False(core.Apps.Contains(new("Spotify")));
        Assert.False(new OvermixCore(_pc.Platform, _store).Apps.Contains(new("Spotify")));
    }

    [Fact]
    public void A_removed_App_comes_back_with_its_setup()
    {
        var core = new OvermixCore(_pc.Platform, _store);
        core.RemoveApp(new("Discord"));

        core.AddApp(core.RemovedApps.Single());

        Assert.Equal(Shortcut.Parse("Ctrl+Alt+Up"), core.Apps.Find(new("Discord"))?.VolumeUp);
        Assert.Empty(core.RemovedApps);
    }

    [Fact]
    public void A_changed_Shortcut_is_remembered()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        core.Shortcuts.SetMicMuteShortcut(Shortcut.Parse("Pause"));

        Assert.Equal("Pause", _store.Saved.MicMuteShortcut);
    }

    [Fact]
    public void An_Apps_program_path_is_remembered_for_its_icon()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        core.RememberPath(new("Discord"), @"C:\Users\me\AppData\Local\Discord\Discord.exe");

        Assert.Equal(@"C:\Users\me\AppData\Local\Discord\Discord.exe", new OvermixCore(_pc.Platform, _store).Apps.Items[0].Path);
    }

    [Fact]
    public void The_Panel_remembers_whether_it_was_open()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        core.PanelOpen = false;

        Assert.False(new OvermixCore(_pc.Platform, _store).PanelOpen);
    }
}
