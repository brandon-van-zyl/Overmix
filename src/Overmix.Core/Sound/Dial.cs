namespace Overmix.Core.Sound;

/// <summary>
/// The volume control for one App, or for the Game. <see cref="Program"/> is what it controls right now
/// (the Game Dial has none without a Game), and it has a <see cref="Volume"/> only while that program is
/// Connected: running and using sound.
/// </summary>
public sealed record Dial(DialTarget Target, ProgramName? Program, string Name, Volume? Volume)
{
    public bool IsConnected => Volume is not null;
}

/// <summary>What a Dial controls: the Game, or one App.</summary>
public abstract record DialTarget
{
    private DialTarget()
    {
    }

    public static DialTarget Game { get; } = new GameTarget();

    public static DialTarget For(ProgramName app) => new AppTarget(app);

    public sealed record GameTarget : DialTarget;

    public sealed record AppTarget(ProgramName Program) : DialTarget;
}
