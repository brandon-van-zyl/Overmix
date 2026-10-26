using Overmix.Core;
using Overmix.Core.Settings;

namespace Overmix.Fakes;

public sealed class FakeStartWithWindows : IStartWithWindows
{
    public bool IsOn { get; private set; }

    public void Set(bool on) => IsOn = on;
}

public sealed class FakeGameFocus : IGameFocus
{
    public int Remembered { get; private set; }

    public int Returned { get; private set; }

    public void Remember() => Remembered++;

    public void Return() => Returned++;
}

/// <summary>Settings kept in memory, so tests never touch the real settings file.</summary>
public sealed class InMemorySettingsStore(OvermixSettings? settings = null) : ISettingsStore
{
    public OvermixSettings Saved { get; private set; } = settings ?? new OvermixSettings();

    public OvermixSettings Load() => Saved;

    public void Save(OvermixSettings settings) => Saved = settings;
}
