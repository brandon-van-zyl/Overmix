using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Overmix.Controls;
using Overmix.Core;
using Overmix.Core.Shortcuts;
using Overmix.Core.Sound;
using Overmix.Fakes;
using Overmix.ViewModels;
using Overmix.Views;
using Overmix.WindowsPlatform;

namespace Overmix.Tests;

public sealed class WindowTests
{
    private readonly FakePc _pc = FakePc.GamingEvening();

    [AvaloniaFact]
    public void The_Panel_shows_the_Game_Dial_then_a_Dial_per_App()
    {
        var window = new PanelWindow { DataContext = CreateViewModel() };
        window.Show();

        var names = window.GetVisualDescendants().OfType<DialView>().Select(view => ((DialViewModel)view.DataContext!).Name);

        Assert.Equal(["VALORANT", "Discord", "Spotify", "Chrome"], names);
    }

    [AvaloniaFact]
    public void The_Panel_turns_red_while_Mic_Mute_is_on()
    {
        var viewModel = CreateViewModel();
        var window = new PanelWindow { DataContext = viewModel };
        window.Show();

        viewModel.ToggleMicMuteCommand.Execute(null);

        Assert.Contains("muted", window.Classes);
        Assert.True(_pc.Microphones.IsMuted("headset-mic"));
    }

    [AvaloniaFact]
    public void Turning_a_Dial_sets_its_programs_volume()
    {
        var viewModel = CreateViewModel();

        viewModel.Dials.Single(dial => dial.Name == "Discord").Volume = 70;

        Assert.Equal(new Volume(70), _pc.Sound.GetVolume(new("Discord")));
    }

    [AvaloniaFact]
    public void Esc_closes_the_Overlay_and_hands_focus_back_to_the_Game()
    {
        var overlay = new OverlayWindow(_pc.GameFocus, spot: null) { DataContext = CreateViewModel() };
        overlay.Open();

        overlay.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);

        Assert.False(overlay.IsVisible);
        Assert.Equal(1, _pc.GameFocus.Remembered);
        Assert.Equal(1, _pc.GameFocus.Returned);
    }

    [AvaloniaFact]
    public void Every_key_the_app_records_works_as_a_Shortcut_on_Windows()
    {
        foreach (var key in Enum.GetValues<Key>())
        {
            if (AvaloniaKeys.NameOf(key) is { } name) Assert.True(KeyNames.TryGetCanonical(name, out _), name);
        }

        foreach (var name in KeyNames.All) Assert.True(KeyCodes.TryGet(name, out _), name);
    }

    private OvermixViewModel CreateViewModel() =>
        new(new OvermixCore(_pc.Platform, new InMemorySettingsStore(FakePc.SampleSettings())), "0.1.0-test");
}
