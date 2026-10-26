using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overmix.Core;

namespace Overmix.ViewModels;

/// <summary>Everything the Panel and the Overlay show. Both windows share this one.</summary>
public sealed partial class OvermixViewModel : ObservableObject
{
    private readonly OvermixCore _core;
    private readonly IconCache _icons;

    public OvermixViewModel(OvermixCore core, string version)
    {
        _core = core;
        _icons = new IconCache(core);
        Settings = new SettingsViewModel(core, _icons, version, () => IsSettingsOpen = false);

        core.Mixer.Changed += (_, _) => SyncDials();
        core.MicMute.Changed += (_, _) => SyncMicMute();
        core.Shortcuts.Changed += (_, _) => SyncMicMute();
        core.OutputSwitch.Changed += (_, _) => SyncOutput();
        core.NowPlaying.Changed += (_, _) => SyncNowPlaying();
        SyncDials();
        SyncMicMute();
        SyncOutput();
        SyncNowPlaying();
    }

    public ObservableCollection<DialViewModel> Dials { get; } = [];

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    /// <summary>True until the first App is added, when only the Game Dial shows.</summary>
    [ObservableProperty]
    public partial bool HasNoApps { get; private set; }

    [ObservableProperty]
    public partial bool IsMicMuted { get; private set; }

    [ObservableProperty]
    public partial string MicStateText { get; private set; } = "";

    [ObservableProperty]
    public partial string MicMuteShortcutText { get; private set; } = "";

    [ObservableProperty]
    public partial string CurrentOutputName { get; private set; } = "";

    [ObservableProperty]
    public partial string OtherOutputName { get; private set; } = "";

    [ObservableProperty]
    public partial bool CanFlipOutput { get; private set; }

    [ObservableProperty]
    public partial bool HasTrack { get; private set; }

    [ObservableProperty]
    public partial string TrackTitle { get; private set; } = "";

    [ObservableProperty]
    public partial string TrackArtist { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsPlaying { get; private set; }

    [RelayCommand]
    private void ToggleMicMute() => _core.MicMute.Toggle();

    [RelayCommand]
    private void FlipOutput() => _core.OutputSwitch.Flip();

    [RelayCommand]
    private void PlayPause() => _core.NowPlaying.PlayPause();

    [RelayCommand]
    private void Next() => _core.NowPlaying.Next();

    [RelayCommand]
    private void Previous() => _core.NowPlaying.Previous();

    [RelayCommand]
    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    partial void OnIsSettingsOpenChanged(bool value)
    {
        if (value) Settings.Load();
    }

    private void SyncDials()
    {
        var dials = _core.Mixer.Dials;
        HasNoApps = _core.Apps.Items.Count == 0;
        if (Dials.Count == dials.Count && Dials.Zip(dials).All(pair => pair.First.Target == pair.Second.Target))
        {
            for (var i = 0; i < dials.Count; i++) Dials[i].Update(dials[i]);
            return;
        }

        // Apps were added, removed or moved: start the row again.
        Dials.Clear();
        foreach (var dial in dials) Dials.Add(new DialViewModel(_core, _icons, dial));
    }

    private void SyncMicMute()
    {
        IsMicMuted = _core.MicMute.IsOn;
        MicStateText = IsMicMuted ? "MIC MUTED" : "MIC LIVE";
        MicMuteShortcutText = _core.Shortcuts.MicMuteShortcut?.ToString() ?? "";
    }

    private void SyncOutput()
    {
        var outputSwitch = _core.OutputSwitch;
        CurrentOutputName = outputSwitch.Current?.Name ?? "No output";
        CanFlipOutput = outputSwitch.Other is not null && outputSwitch.Other != outputSwitch.Current;
        OtherOutputName = outputSwitch.Pair is null ? "Set up in Settings" : outputSwitch.Other?.Name ?? "Not plugged in";
    }

    private void SyncNowPlaying()
    {
        var track = _core.NowPlaying.Track;
        HasTrack = track is not null;
        TrackTitle = track?.Title ?? "";
        TrackArtist = track?.Artist ?? "";
        IsPlaying = _core.NowPlaying.IsPlaying;
    }
}
