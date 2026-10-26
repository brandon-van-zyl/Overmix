using Overmix.Core.Media;
using Overmix.Core.Settings;
using Overmix.Core.Shortcuts;
using Overmix.Core.Sound;
using Overmix.Core.Voice;

namespace Overmix.Core;

/// <summary>Everything Overmix does, wired together over a <see cref="Platform"/>. The UI talks only to this.</summary>
public sealed class OvermixCore
{
    private readonly ISettingsStore _store;
    private readonly List<App> _removedApps;
    private OvermixSettings _settings;

    public OvermixCore(Platform platform, ISettingsStore store)
    {
        _store = store;
        _settings = store.Load();
        Platform = platform;
        Apps = new AppList(_settings.Apps.Select(ToApp).OfType<App>());
        _removedApps = _settings.RemovedApps.Select(ToApp).OfType<App>().ToList();
        Game = new GameTracker(platform.MainScreen);
        Mixer = new Mixer(platform.Sound, Apps, Game);
        MicMute = new MicMute(platform.Microphones);
        OutputSwitch = new OutputSwitch(platform.Outputs, ToPair(_settings.OutputFirst, _settings.OutputSecond));
        Shortcuts = new ShortcutDispatcher(
            platform.ShortcutKeys, Apps, Mixer, MicMute, ToShortcut(_settings.PopupShortcut), ToShortcut(_settings.MicMuteShortcut));

        Apps.Changed += (_, _) => Save();
        OutputSwitch.Changed += (_, _) => Save();
        Shortcuts.Changed += (_, _) => Save();
        MicMute.Changed += (_, _) => ShowMuteBorder();

        // Only for your own presses: no chime as Overmix starts and finds the mic already muted.
        MicMute.Switched += (_, _) =>
        {
            if (_settings.MuteChime) platform.MuteChime.Play(MicMute.IsOn);
        };
        ShowMuteBorder();

        platform.Sound.Started += (_, _) => AddNewPrograms();
        AddNewPrograms();
    }

    public Platform Platform { get; }

    public AppList Apps { get; }

    public GameTracker Game { get; }

    public Mixer Mixer { get; }

    public MicMute MicMute { get; }

    public OutputSwitch OutputSwitch { get; }

    public ShortcutDispatcher Shortcuts { get; }

    public INowPlaying NowPlaying => Platform.NowPlaying;

    public bool StartsWithWindows
    {
        get => Platform.StartWithWindows.IsOn;
        set => Platform.StartWithWindows.Set(value);
    }

    /// <summary>Whether the Panel was open when Overmix last ran, so it starts the same way.</summary>
    public bool PanelOpen
    {
        get => _settings.PanelOpen;
        set => Save(_settings with { PanelOpen = value });
    }

    /// <summary>Whether the Mute Chime plays as Mic Mute turns on or off.</summary>
    public bool MuteChimeOn
    {
        get => _settings.MuteChime;
        set => Save(_settings with { MuteChime = value });
    }

    /// <summary>Whether the Mute Border shows while Mic Mute is on.</summary>
    public bool MuteBorderOn
    {
        get => _settings.MuteBorder;
        set
        {
            Save(_settings with { MuteBorder = value });
            ShowMuteBorder();
        }
    }

    public WindowSpot? PanelSpot
    {
        get => _settings.PanelSpot;
        set => Save(_settings with { PanelSpot = value });
    }

    public WindowSpot? OverlaySpot
    {
        get => _settings.OverlaySpot;
        set => Save(_settings with { OverlaySpot = value });
    }

    public IProgramIcons Icons => Platform.Icons;

    /// <summary>Notes where an App's program lives, so its icon can show even while it isn't running.</summary>
    public void RememberPath(ProgramName program, string path)
    {
        if (Apps.Find(program) is { } app && app.Path != path) Apps.Update(app with { Path = path });
    }

    /// <summary>Apps you removed, which don't come back by themselves.</summary>
    public IReadOnlyList<App> RemovedApps => _removedApps;

    /// <summary>Takes an App off the list for good: it won't be added again when its program next plays sound.</summary>
    public void RemoveApp(ProgramName program)
    {
        if (Apps.Find(program) is not { } app) return;
        _removedApps.RemoveAll(removed => removed.Program == program);
        _removedApps.Add(app);
        Apps.Remove(program);
    }

    /// <summary>Adds an App by hand: one removed earlier, with its old setup, or a program chosen with Browse.</summary>
    public void AddApp(App app)
    {
        _removedApps.RemoveAll(removed => removed.Program == app.Program);
        if (!Apps.Add(app)) Save();
    }

    /// <summary>Every program using sound becomes an App, like Windows' volume mixer, unless you've removed it.</summary>
    private void AddNewPrograms()
    {
        foreach (var program in Platform.Sound.UsingSound)
        {
            if (!Apps.Contains(program.Program) && !_removedApps.Exists(removed => removed.Program == program.Program))
                Apps.Add(new App(program.Program, program.Name));
        }
    }

    private void ShowMuteBorder()
    {
        if (MicMute.IsOn && _settings.MuteBorder) Platform.MuteBorder.Show();
        else Platform.MuteBorder.Hide();
    }

    private void Save() => Save(_settings);

    private void Save(OvermixSettings settings)
    {
        _settings = settings with
        {
            Apps = Apps.Items.Select(ToSettings).ToList(),
            RemovedApps = _removedApps.Select(ToSettings).ToList(),
            PopupShortcut = Shortcuts.PopupShortcut?.ToString(),
            MicMuteShortcut = Shortcuts.MicMuteShortcut?.ToString(),
            OutputFirst = OutputSwitch.Pair?.First.Value,
            OutputSecond = OutputSwitch.Pair?.Second.Value,
        };
        _store.Save(_settings);
    }

    private static AppSettings ToSettings(App app) =>
        new(app.Program.Value, app.Name, app.VolumeUp?.ToString(), app.VolumeDown?.ToString(), app.Path);

    private static App? ToApp(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Program)) return null;
        var program = new ProgramName(settings.Program);
        return new App(program, string.IsNullOrWhiteSpace(settings.Name) ? program.Value : settings.Name)
        {
            Path = string.IsNullOrWhiteSpace(settings.Path) ? null : settings.Path,
            VolumeUp = ToShortcut(settings.VolumeUp),
            VolumeDown = ToShortcut(settings.VolumeDown),
        };
    }

    private static OutputPair? ToPair(string? first, string? second) =>
        string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second)
            ? null
            : new OutputPair(new DeviceId(first), new DeviceId(second));

    private static Shortcut? ToShortcut(string? text) => Shortcut.TryParse(text, out var shortcut) ? shortcut : null;
}
