using Overmix.Core;
using Overmix.Core.Settings;
using Overmix.Core.Voice;

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

public sealed class FakeMuteChime : IMuteChime
{
    /// <summary>Every chime played: true for muting, false for unmuting.</summary>
    public List<bool> Played { get; } = [];

    public void Play(bool muted) => Played.Add(muted);
}

public sealed class FakeMuteBorder : IMuteBorder
{
    public bool IsShown { get; private set; }

    public void Show() => IsShown = true;

    public void Hide() => IsShown = false;
}

/// <summary>Settings kept in memory, so tests never touch the real settings file.</summary>
public sealed class InMemorySettingsStore(OvermixSettings? settings = null) : ISettingsStore
{
    public OvermixSettings Saved { get; private set; } = settings ?? new OvermixSettings();

    public OvermixSettings Load() => Saved;

    public void Save(OvermixSettings settings) => Saved = settings;
}
