using Overmix.Core.Sound;

namespace Overmix.Fakes;

/// <summary>A pretend main screen that programs fill and quit.</summary>
public sealed class FakeMainScreen : IMainScreen
{
    public FullScreenProgram? Current { get; private set; }

    public event EventHandler<FullScreenProgram>? Filled;

    public event EventHandler<ProgramName>? ProgramQuit;

    /// <summary>A program starts filling the main screen.</summary>
    public void Fill(string program, string? name = null)
    {
        var filling = new FullScreenProgram(new ProgramName(program), name ?? program);
        Current = filling;
        Filled?.Invoke(this, filling);
    }

    /// <summary>A program quits.</summary>
    public void Quit(string program)
    {
        var quitting = new ProgramName(program);
        if (Current?.Program == quitting) Current = null;
        ProgramQuit?.Invoke(this, quitting);
    }
}
