using SoundSwap.Core.Hotkeys;
using Xunit;

namespace SoundSwap.Core.Tests;

public class HotkeyGestureTests
{
    [Fact]
    public void Parse_IsCaseInsensitive_AndNormalizes()
    {
        Assert.True(HotkeyGesture.TryParse("alt + CTRL + Right", out var g));
        Assert.Equal("Ctrl+Alt+Right", g.ToString());
        Assert.Equal("Ctrl + Alt + →", g.Display);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Foo+A")]
    public void Parse_RejectsInvalid(string? text) => Assert.False(HotkeyGesture.TryParse(text, out _));

    [Fact]
    public void Display_ShowsDigitsWithoutPrefix()
    {
        HotkeyGesture.TryParse("Ctrl+Alt+D1", out var g);
        Assert.Equal("Ctrl + Alt + 1", g.Display);
    }
}
