using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overmix.Core;
using Overmix.Core.Shortcuts;
using Overmix.Core.Sound;

namespace Overmix.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly OvermixCore _core;
    private readonly IconCache _icons;
    private readonly Action _close;
    private bool _loading;

    public SettingsViewModel(OvermixCore core, IconCache icons, string version, Action close)
    {
        _core = core;
        _icons = icons;
        _close = close;
        VersionText = $"Overmix {version}";
        core.Apps.Changed += (_, _) => SyncApps(rebuild: false);
        core.Shortcuts.Changed += (_, _) => SyncShortcutWarning();
    }

    public ObservableCollection<AppRowViewModel> Apps { get; } = [];

    /// <summary>Apps you removed, which can be brought back with their old setup.</summary>
    public ObservableCollection<App> RemovedApps { get; } = [];

    public ObservableCollection<SoundOutput> Outputs { get; } = [];

    public string VersionText { get; }

    [ObservableProperty]
    public partial App? SelectedRemovedApp { get; set; }

    [ObservableProperty]
    public partial bool HasRemovedApps { get; private set; }

    [ObservableProperty]
    public partial Shortcut? PopupShortcut { get; set; }

    [ObservableProperty]
    public partial Shortcut? MicMuteShortcut { get; set; }

    [ObservableProperty]
    public partial bool KeyboardHook { get; set; }

    [ObservableProperty]
    public partial bool MuteChime { get; set; }

    [ObservableProperty]
    public partial bool MuteBorder { get; set; }

    [ObservableProperty]
    public partial SoundOutput? FirstOutput { get; set; }

    [ObservableProperty]
    public partial SoundOutput? SecondOutput { get; set; }

    [ObservableProperty]
    public partial bool StartsWithWindows { get; set; }

    [ObservableProperty]
    public partial string ShortcutWarning { get; private set; } = "";

    [ObservableProperty]
    public partial bool HasShortcutWarning { get; private set; }

    /// <summary>Fills everything in from Overmix's current state, each time Settings opens.</summary>
    public void Load()
    {
        _loading = true;
        try
        {
            SyncApps(rebuild: true);

            Outputs.Clear();
            foreach (var output in _core.OutputSwitch.Available) Outputs.Add(output);
            FirstOutput = Outputs.FirstOrDefault(output => output.Id == _core.OutputSwitch.Pair?.First);
            SecondOutput = Outputs.FirstOrDefault(output => output.Id == _core.OutputSwitch.Pair?.Second);

            PopupShortcut = _core.Shortcuts.PopupShortcut;
            MicMuteShortcut = _core.Shortcuts.MicMuteShortcut;
            KeyboardHook = _core.Shortcuts.UseKeyboardHook;
            MuteChime = _core.MuteChimeOn;
            MuteBorder = _core.MuteBorderOn;
            StartsWithWindows = _core.StartsWithWindows;
            SyncShortcutWarning();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Adds a program chosen with Browse, which can be done before it has ever played sound.</summary>
    public void AddProgramFile(string path)
    {
        var program = new ProgramName(Path.GetFileName(path));
        var description = FileVersionInfo.GetVersionInfo(path).FileDescription;
        _core.AddApp(new App(program, string.IsNullOrWhiteSpace(description) ? program.Value : description.Trim()) { Path = path });
    }

    [RelayCommand]
    private void Done() => _close();

    [RelayCommand]
    private void BringBack()
    {
        if (SelectedRemovedApp is { } app) _core.AddApp(app);
    }

    partial void OnPopupShortcutChanged(Shortcut? value)
    {
        if (!_loading) _core.Shortcuts.SetPopupShortcut(value);
    }

    partial void OnMicMuteShortcutChanged(Shortcut? value)
    {
        if (!_loading) _core.Shortcuts.SetMicMuteShortcut(value);
    }

    partial void OnKeyboardHookChanged(bool value)
    {
        if (!_loading) _core.Shortcuts.UseKeyboardHook = value;
    }

    partial void OnMuteChimeChanged(bool value)
    {
        if (!_loading) _core.MuteChimeOn = value;
    }

    partial void OnMuteBorderChanged(bool value)
    {
        if (!_loading) _core.MuteBorderOn = value;
    }

    partial void OnFirstOutputChanged(SoundOutput? value) => ChoosePair();

    partial void OnSecondOutputChanged(SoundOutput? value) => ChoosePair();

    partial void OnStartsWithWindowsChanged(bool value)
    {
        if (!_loading) _core.StartsWithWindows = value;
    }

    private void ChoosePair()
    {
        if (_loading) return;
        _core.OutputSwitch.Choose(
            FirstOutput is { } first && SecondOutput is { } second && first.Id != second.Id ? new OutputPair(first.Id, second.Id) : null);
    }

    private void SyncApps(bool rebuild)
    {
        RemovedApps.Clear();
        foreach (var removed in _core.RemovedApps.OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)) RemovedApps.Add(removed);
        SelectedRemovedApp = RemovedApps.FirstOrDefault();
        HasRemovedApps = RemovedApps.Count > 0;

        // Edits to a row change the App in place; only a different list of Apps needs new rows.
        var apps = _core.Apps.Items;
        if (!rebuild && Apps.Select(row => row.Program).SequenceEqual(apps.Select(app => app.Program))) return;
        Apps.Clear();
        foreach (var app in apps) Apps.Add(new AppRowViewModel(_core, _icons, app));
    }

    private void SyncShortcutWarning()
    {
        var taken = _core.Shortcuts.Unavailable;
        HasShortcutWarning = taken.Count > 0;
        ShortcutWarning = !HasShortcutWarning ? ""
            : _core.Shortcuts.UseKeyboardHook ? $"{string.Join(", ", taken)} can't be used. Pick a different one."
            : $"Another app already uses {string.Join(", ", taken)}. Pick a different one, or turn on Catch blocked Shortcuts.";
    }
}

/// <summary>One App in Settings: its name and its volume Shortcuts.</summary>
public sealed partial class AppRowViewModel : ObservableObject
{
    private readonly OvermixCore _core;
    private App _app;

    public AppRowViewModel(OvermixCore core, IconCache icons, App app)
    {
        _core = core;
        _app = app;
        Name = app.Name;
        VolumeUp = app.VolumeUp;
        VolumeDown = app.VolumeDown;
        _ = LoadIconAsync(icons);
    }

    public ProgramName Program => _app.Program;

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial Shortcut? VolumeUp { get; set; }

    [ObservableProperty]
    public partial Shortcut? VolumeDown { get; set; }

    [ObservableProperty]
    public partial Bitmap? Icon { get; private set; }

    /// <summary>Removes the App for good; it won't be added again when it next plays sound.</summary>
    [RelayCommand]
    private void Remove() => _core.RemoveApp(_app.Program);

    partial void OnNameChanged(string value)
    {
        if (_app is not null && !string.IsNullOrWhiteSpace(value)) Save(_app with { Name = value.Trim() });
    }

    partial void OnVolumeUpChanged(Shortcut? value)
    {
        if (_app is not null) Save(_app with { VolumeUp = value });
    }

    partial void OnVolumeDownChanged(Shortcut? value)
    {
        if (_app is not null) Save(_app with { VolumeDown = value });
    }

    private void Save(App app)
    {
        if (app == _app) return;
        _app = app;
        _core.Apps.Update(app);
    }

    private async Task LoadIconAsync(IconCache icons)
    {
        try
        {
            Icon = await icons.GetAsync(_app.Program);
        }
        catch (Exception e)
        {
            Trace.TraceWarning($"Overmix: no icon for {_app.Program}: {e.Message}");
        }
    }
}
