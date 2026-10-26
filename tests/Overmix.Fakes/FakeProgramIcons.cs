using Overmix.Core.Sound;

namespace Overmix.Fakes;

/// <summary>Pretend icons: a plain coloured disc for each program given one.</summary>
public sealed class FakeProgramIcons : IProgramIcons
{
    private readonly Dictionary<ProgramName, (byte Red, byte Green, byte Blue)> _colours = [];

    /// <summary>Gives a program an icon, a disc of one colour.</summary>
    public void Give(string program, byte red, byte green, byte blue) => _colours[new ProgramName(program)] = (red, green, blue);

    public Task<ProgramIcon?> GetAsync(ProgramName program, string? knownPath) =>
        Task.FromResult(_colours.TryGetValue(program, out var colour) ? Disc(colour, $@"C:\Programs\{program}.exe") : null);

    private static ProgramIcon Disc((byte Red, byte Green, byte Blue) colour, string path)
    {
        const int size = 64;
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - size / 2 + 0.5;
                var dy = y - size / 2 + 0.5;
                if (dx * dx + dy * dy > (size / 2 - 2) * (size / 2 - 2)) continue;
                var i = (y * size + x) * 4;
                (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = (colour.Blue, colour.Green, colour.Red, 255);
            }
        }

        return new ProgramIcon(size, size, pixels, path);
    }
}
