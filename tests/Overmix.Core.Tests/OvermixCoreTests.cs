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

        Assert.Equal(["Discord", "Spotify", "Chrome"], core.Apps.Items.Select(app => app.Name));
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
    public void Programs_to_add_are_those_using_sound_that_arent_Apps_yet()
    {
        var core = new OvermixCore(_pc.Platform, _store);

        Assert.Equal(["VALORANT"], core.ProgramsToAdd.Select(program => program.Name));
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
