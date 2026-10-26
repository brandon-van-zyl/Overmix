using System.Diagnostics;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Overmix.Core;
using Overmix.Core.Sound;

namespace Overmix.ViewModels;

/// <summary>One Dial on screen. Turning it changes its program's volume.</summary>
public sealed partial class DialViewModel : ObservableObject
{
    private readonly OvermixCore _core;
    private readonly IconCache _icons;
    private ProgramName? _program;
    private bool _updating;

    public DialViewModel(OvermixCore core, IconCache icons, Dial dial)
    {
        _core = core;
        _icons = icons;
        Target = dial.Target;
        Update(dial);
    }

    public DialTarget Target { get; }

    public bool IsGame => Target is DialTarget.GameTarget;

    [ObservableProperty]
    public partial string Name { get; private set; } = "";

    [ObservableProperty]
    public partial string Monogram { get; private set; } = "";

    [ObservableProperty]
    public partial string Tip { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsConnected { get; private set; }

    [ObservableProperty]
    public partial int Volume { get; set; }

    [ObservableProperty]
    public partial Bitmap? Icon { get; private set; }

    public void Update(Dial dial)
    {
        var wasConnected = IsConnected;
        _updating = true;
        try
        {
            Name = dial.Name.Length > 0 ? dial.Name : "No game";
            Monogram = dial.Name.Length > 0 ? dial.Name[..1].ToUpperInvariant() : "";
            IsConnected = dial.IsConnected;
            if (dial.Volume is { } volume) Volume = volume.Percent;
            Tip = dial switch
            {
                { IsConnected: true } => $"{Name}: {Volume}%",
                { Program: null } => "The game you play shows up here",
                _ => $"{Name} isn't running",
            };
        }
        finally
        {
            _updating = false;
        }

        // Look for the icon when the program changes, and again once it's running if it wasn't found before.
        if (dial.Program != _program)
        {
            _program = dial.Program;
            Icon = null;
            _ = LoadIconAsync(dial.Program);
        }
        else if (Icon is null && dial.IsConnected && !wasConnected)
        {
            _ = LoadIconAsync(dial.Program);
        }
    }

    partial void OnVolumeChanged(int value)
    {
        if (!_updating) _core.Mixer.SetVolume(Target, new Volume(value));
    }

    private async Task LoadIconAsync(ProgramName? program)
    {
        if (program is not { } wanted) return;
        try
        {
            var icon = await _icons.GetAsync(wanted);
            if (_program == wanted && icon is not null) Icon = icon;
        }
        catch (Exception e)
        {
            // A missing icon only costs the picture; the Dial still works.
            Trace.TraceWarning($"Overmix: no icon for {wanted}: {e.Message}");
        }
    }
}
