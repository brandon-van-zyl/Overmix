namespace Overmix.Core.Settings;

public interface ISettingsStore
{
    OvermixSettings Load();

    void Save(OvermixSettings settings);
}
