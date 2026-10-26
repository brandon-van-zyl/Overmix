using Overmix.Core.Shortcuts;

namespace Overmix.Core.Tests;

public sealed class ShortcutTests
{
    [Theory]
    [InlineData("Ctrl+Alt+M")]
    [InlineData("Shift+F9")]
    [InlineData("NumPad1")]
    [InlineData("Ctrl+`")]
    [InlineData("Ctrl+Alt+Shift+Win+PageDown")]
    public void Shortcuts_read_back_the_way_they_were_written(string text)
    {
        Assert.Equal(text, Shortcut.Parse(text).ToString());
    }

    [Fact]
    public void Reading_ignores_case_and_spaces()
    {
        Assert.Equal(new Shortcut(ShortcutModifiers.Ctrl | ShortcutModifiers.Alt, "M"), Shortcut.Parse(" ctrl + alt + m "));
    }

    [Fact]
    public void Modifiers_always_print_in_the_same_order()
    {
        Assert.Equal("Ctrl+Alt+Shift+X", Shortcut.Parse("Shift+Alt+Ctrl+X").ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Hyper+M")]
    [InlineData("Ctrl+Banana")]
    [InlineData("Escape")]
    public void Nonsense_isnt_a_Shortcut(string text)
    {
        Assert.False(Shortcut.TryParse(text, out _));
    }
}
