using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class FieldScrollerTests
{
    [Fact]
    public void CameraStaysStillUntilPlayerCrossesTheScreenBuffer()
    {
        var scroller = new FieldScroller(180f, 32f, 12f);
        scroller.SetFieldBounds(-170f, 350f);

        scroller.Follow(105f);
        scroller.Follow(44f); // Sprite top is exactly 32 units from the screen edge.
        Assert.Equal(0f, scroller.Top);

        scroller.Follow(32f);
        Assert.Equal(-12f, scroller.Top);
        Assert.Equal(32f, 32f - 12f - scroller.Top);

        scroller.Follow(80f);
        Assert.Equal(-12f, scroller.Top);

        scroller.Follow(145f);
        Assert.Equal(-3f, scroller.Top);
        Assert.Equal(32f, 180f - (145f - scroller.Top));
    }

    [Fact]
    public void CameraStopsAtFieldBoundsAndResetsForANewGame()
    {
        var scroller = new FieldScroller(180f, 32f, 12f);
        scroller.SetFieldBounds(-170f, 350f);

        scroller.Follow(-300f);
        Assert.Equal(-170f, scroller.Top);

        scroller.Follow(500f);
        Assert.Equal(170f, scroller.Top);

        scroller.Reset();
        Assert.Equal(0f, scroller.Top);
    }
}
