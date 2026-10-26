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
    private OvermixSettings _settings;

    public OvermixCore(Platform platform, ISettingsStore store)
    {
        _store = store;
        _settings = store.Load();
        Platform = platform;
        Apps = new AppList(_settings.Apps.Select(ToApp).OfType<App>());
        Game = new GameTracker(platform.MainScreen, Apps);
        Mixer = new Mixer(platform.Sound, Apps, Game);
        MicMute = new MicMute(platform.Microphones);
        OutputSwitch = new OutputSwitch(platform.Outputs, ToPair(_settings.OutputFirst, _settings.OutputSecond));
        Shortcuts = new ShortcutDispatcher(
            platform.ShortcutKeys, Apps, Mixer, MicMute, ToShortcut(_settings.PopupShortcut), ToShortcut(_settings.MicMuteShortcut));

        Apps.Changed += (_, _) => Save();
        OutputSwitch.Changed += (_, _) => Save();
        Shortcuts.Changed += (_, _) => Save();
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

    /// <summary>Programs using sound right now that aren't Apps yet: the choices for adding one.</summary>
    public IReadOnlyList<SoundProgram> ProgramsToAdd =>
        Platform.Sound.UsingSound
            .Where(program => !Apps.Contains(program.Program))
            .OrderBy(program => program.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private void Save() => Save(_settings);

    private void Save(OvermixSettings settings)
    {
        _settings = settings with
        {
            Apps = Apps.Items
                .Select(app => new AppSettings(
                    app.Program.Value, app.Name, app.VolumeUp?.ToString(), app.VolumeDown?.ToString(), app.Path))
                .ToList(),
            PopupShortcut = Shortcuts.PopupShortcut?.ToString(),
            MicMuteShortcut = Shortcuts.MicMuteShortcut?.ToString(),
            OutputFirst = OutputSwitch.Pair?.First.Value,
            OutputSecond = OutputSwitch.Pair?.Second.Value,
        };
        _store.Save(_settings);
    }

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
