namespace Overmix.Core.Sound;

/// <summary>A program's icon as 32-bit BGRA pixels with premultiplied alpha, and the file it came from.</summary>
public sealed record ProgramIcon(int Width, int Height, byte[] Pixels, string SourcePath);

/// <summary>Finds programs' icons: the same art as their desktop shortcuts.</summary>
public interface IProgramIcons
{
    /// <summary>
    /// The program's icon, taken from <paramref name="knownPath"/> or else from the running program.
    /// Null if neither works, for example because the program isn't running.
    /// </summary>
    Task<ProgramIcon?> GetAsync(ProgramName program, string? knownPath);
}
