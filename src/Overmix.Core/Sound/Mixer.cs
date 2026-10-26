namespace Overmix.Core.Sound;

/// <summary>The Dials: the Game Dial first, then one per App, kept in step with the programs using sound.</summary>
public sealed class Mixer
{
    private readonly IProgramSound _sound;
    private readonly AppList _apps;
    private readonly GameTracker _game;

    public Mixer(IProgramSound sound, AppList apps, GameTracker game)
    {
        _sound = sound;
        _apps = apps;
        _game = game;
        sound.Started += (_, _) => Refresh();
        sound.Stopped += (_, _) => Refresh();
        sound.VolumeChanged += (_, _) => Refresh();
        apps.Changed += (_, _) => Refresh();
        game.Changed += (_, _) => Refresh();
        Dials = Build();
    }

    public IReadOnlyList<Dial> Dials { get; private set; }

    public event EventHandler? Changed;

    public void SetVolume(DialTarget target, Volume volume)
    {
        if (ProgramOf(target) is { } program) _sound.SetVolume(program, volume);
    }

    /// <summary>Turns a Dial up (positive) or down (negative) by a number of percentage points.</summary>
    public void Nudge(DialTarget target, int by)
    {
        if (ProgramOf(target) is { } program && _sound.GetVolume(program) is { } volume)
            _sound.SetVolume(program, volume.Nudge(by));
    }

    private ProgramName? ProgramOf(DialTarget target) => target switch
    {
        DialTarget.AppTarget app => app.Program,
        _ => _game.Current?.Program,
    };

    private void Refresh()
    {
        var dials = Build();
        if (dials.SequenceEqual(Dials)) return;
        Dials = dials;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private List<Dial> Build()
    {
        var game = _game.Current;
        var gameDial = new Dial(DialTarget.Game, game?.Program, game?.Name ?? "", game is null ? null : _sound.GetVolume(game.Program));
        return
        [
            gameDial,
            .. _apps.Items.Select(app => new Dial(DialTarget.For(app.Program), app.Program, app.Name, _sound.GetVolume(app.Program))),
        ];
    }
}
