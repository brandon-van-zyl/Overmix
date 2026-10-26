namespace Overmix.Core.Sound;

/// <summary>A program filling the main screen, such as a game running borderless.</summary>
public sealed record FullScreenProgram(ProgramName Program, string Name);

/// <summary>Watches which program fills the main screen. Raises events on the UI thread.</summary>
public interface IMainScreen
{
    /// <summary>The program filling the main screen right now, if any.</summary>
    FullScreenProgram? Current { get; }

    /// <summary>A program started filling the main screen.</summary>
    event EventHandler<FullScreenProgram>? Filled;

    /// <summary>A program that filled the main screen has quit.</summary>
    event EventHandler<ProgramName>? ProgramQuit;
}
