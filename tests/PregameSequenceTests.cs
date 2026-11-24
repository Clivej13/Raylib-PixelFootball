using RaylibPixelFootball.Application;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class PregameSequenceTests
{
    [Fact]
    public void StartRunsSkylineStadiumAndFieldBeforeGameplay()
    {
        var sequence = new PregameSequence();
        Assert.Equal(PregameStage.Skyline, sequence.Stage);
        Assert.False(sequence.IsComplete);

        sequence.Update(PregameSequence.SkylineSeconds);
        Assert.Equal(PregameStage.Stadium, sequence.Stage);
        Assert.Equal(0f, sequence.StageProgress);

        sequence.Update(PregameSequence.StadiumSeconds);
        Assert.Equal(PregameStage.FieldZoom, sequence.Stage);
        Assert.Equal(0f, sequence.StageProgress);

        sequence.Update(PregameSequence.FieldZoomSeconds);
        Assert.Equal(PregameStage.Complete, sequence.Stage);
        Assert.True(sequence.IsComplete);
        Assert.Equal(1f, sequence.StageProgress);

        sequence.Reset();
        Assert.Equal(PregameStage.Skyline, sequence.Stage);
        sequence.Skip();
        Assert.True(sequence.IsComplete);
    }
}
