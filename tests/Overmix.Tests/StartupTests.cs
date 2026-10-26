using System.Runtime.CompilerServices;

namespace Overmix.Tests;

public sealed class StartupTests
{
    private const string LogPath = @"C:\Users\me\AppData\Roaming\Overmix\overmix.log";

    [Fact]
    public void A_failed_start_says_what_went_wrong_and_where()
    {
        var message = Program.StartFailedMessage(Caught(() => LoadSettings(null)), LogPath);

        Assert.StartsWith("Overmix couldn't start.\n\nSystem.ArgumentNullException: Value cannot be null. (Parameter 'path')", message);
        Assert.Contains($"{nameof(StartupTests)}.{nameof(LoadSettings)}", message);
        Assert.EndsWith($"Press Ctrl+C to copy this message. Everything is in {LogPath}", message);
    }

    [Fact]
    public void A_long_error_is_cut_short_to_fit_on_screen()
    {
        var message = Program.StartFailedMessage(Caught(() => Dig(40)), LogPath);

        Assert.Contains("\n…\n", message);
        Assert.InRange(message.Split('\n').Length, 10, 21);
    }

    private static Exception Caught(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            return e;
        }

        throw new InvalidOperationException("Nothing went wrong.");
    }

    // Kept out of line so it shows in the stack trace, as a real failing method would.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadSettings(string? path) => ArgumentNullException.ThrowIfNull(path);

    private static void Dig(int depth)
    {
        if (depth == 0) throw new InvalidOperationException("Deep down");
        Dig(depth - 1);
    }
}
