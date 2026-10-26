namespace Overmix.Core.Sound;

/// <summary>A program that is running and using sound, with the name people know it by.</summary>
public sealed record SoundProgram(ProgramName Program, string Name);

/// <summary>The sound of each program on the PC: Windows' volume mixer. Raises events on the UI thread.</summary>
public interface IProgramSound
{
    /// <summary>Programs that are running and using sound right now.</summary>
    IReadOnlyCollection<SoundProgram> UsingSound { get; }

    /// <summary>The program's volume, or null if it isn't using sound.</summary>
    Volume? GetVolume(ProgramName program);

    /// <summary>Sets the volume of every sound the program makes. Does nothing if it isn't using sound.</summary>
    void SetVolume(ProgramName program, Volume volume);

    /// <summary>A program started using sound.</summary>
    event EventHandler<ProgramName>? Started;

    /// <summary>A program stopped using sound, usually because it quit.</summary>
    event EventHandler<ProgramName>? Stopped;

    /// <summary>A program's volume changed, from Overmix or from anywhere else.</summary>
    event EventHandler<ProgramName>? VolumeChanged;
}
