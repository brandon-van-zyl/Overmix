using Overmix.Core.Settings;
using Overmix.Fakes;

namespace Overmix.Core.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("overmix-tests-");

    private string SettingsPath => Path.Combine(_folder.FullName, "Overmix", "settings.json");

    public void Dispose() => _folder.Delete(recursive: true);

    [Fact]
    public void A_missing_file_gives_the_defaults()
    {
        var settings = new JsonSettingsStore(SettingsPath).Load();

        Assert.Empty(settings.Apps);
        Assert.True(settings.PanelOpen);
        Assert.Equal("Ctrl+Alt+O", settings.PopupShortcut);
        Assert.Equal("Ctrl+Alt+M", settings.MicMuteShortcut);
    }

    [Fact]
    public void Saved_settings_load_back_the_same()
    {
        var saved = FakePc.SampleSettings() with { PanelOpen = false, PanelSpot = new WindowSpot(1920, 40), PopupShortcut = null };

        new JsonSettingsStore(SettingsPath).Save(saved);
        var loaded = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(saved.Apps, loaded.Apps);
        Assert.Equal(saved with { Apps = loaded.Apps }, loaded);
    }

    [Fact]
    public void An_unreadable_file_is_kept_aside_and_the_defaults_are_used()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ not json");

        var settings = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(new OvermixSettings(), settings);
        Assert.True(File.Exists(SettingsPath + ".broken"));
    }
}
