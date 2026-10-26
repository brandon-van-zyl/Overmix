using Overmix.Core.Sound;

namespace Overmix.Core.Tests;

public sealed class ProgramNameTests
{
    [Fact]
    public void Case_and_the_exe_ending_dont_matter()
    {
        Assert.Equal(new ProgramName("discord"), new ProgramName("Discord.exe"));
    }

    [Fact]
    public void Works_as_a_dictionary_key_whatever_the_case()
    {
        var volumes = new Dictionary<ProgramName, int> { [new("Spotify")] = 30 };

        Assert.Equal(30, volumes[new("SPOTIFY.EXE")]);
    }
}
