namespace Overmix.Core.Sound;

/// <summary>
/// Keeps track of the Game: the most recent program to fill the main screen. It stays the Game, even while
/// the Overlay is open, until it quits or another program fills the main screen.
/// </summary>
public sealed class GameTracker
{
    public GameTracker(IMainScreen screen)
    {
        screen.Filled += (_, program) => Set(new Game(program.Program, program.Name));
        screen.ProgramQuit += (_, program) =>
        {
            if (Current?.Program == program) Set(null);
        };
        if (screen.Current is { } now) Current = new Game(now.Program, now.Name);
    }

    public Game? Current { get; private set; }

    public event EventHandler? Changed;

    private void Set(Game? game)
    {
        if (game == Current) return;
        Current = game;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
