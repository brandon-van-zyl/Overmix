namespace Overmix.Core.Sound;

/// <summary>
/// Keeps track of the Game. A program stays the Game, even while the Overlay is open, until it quits,
/// another program fills the main screen, or it's added as an App.
/// </summary>
public sealed class GameTracker
{
    private readonly AppList _apps;

    public GameTracker(IMainScreen screen, AppList apps)
    {
        _apps = apps;
        screen.Filled += (_, program) => Consider(program);
        screen.ProgramQuit += (_, program) =>
        {
            if (Current?.Program == program) Set(null);
        };
        apps.Changed += (_, _) =>
        {
            if (Current is { } game && apps.Contains(game.Program)) Set(null);
        };
        if (screen.Current is { } now) Consider(now);
    }

    public Game? Current { get; private set; }

    public event EventHandler? Changed;

    private void Consider(FullScreenProgram program)
    {
        if (!_apps.Contains(program.Program)) Set(new Game(program.Program, program.Name));
    }

    private void Set(Game? game)
    {
        if (game == Current) return;
        Current = game;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
