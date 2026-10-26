using Avalonia.Headless.XUnit;
using Overmix.Core;
using Overmix.Fakes;
using Overmix.ViewModels;

namespace Overmix.Tests;

public sealed class IconTests
{
    [AvaloniaFact]
    public void Every_Dial_with_a_known_icon_shows_it()
    {
        var pc = FakePc.GamingEvening();
        var viewModel = new OvermixViewModel(new OvermixCore(pc.Platform, new InMemorySettingsStore(FakePc.SampleSettings())), "test");

        var withIcons = viewModel.Dials.Where(dial => dial.Icon is not null).Select(dial => dial.Name);

        Assert.Equal(["VALORANT", "Discord", "Spotify"], withIcons);
    }

    [AvaloniaFact]
    public void Settings_lists_each_App_once()
    {
        var pc = FakePc.GamingEvening();
        var viewModel = new OvermixViewModel(new OvermixCore(pc.Platform, new InMemorySettingsStore(FakePc.SampleSettings())), "test");

        viewModel.ToggleSettingsCommand.Execute(null);

        Assert.Equal(["Discord", "Spotify", "Chrome", "VALORANT"], viewModel.Settings.Apps.Select(row => row.Name));
    }
}
