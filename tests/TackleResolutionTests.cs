using RaylibPixelFootball.Game;
using RaylibPixelFootball.TwoD;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class TackleResolutionTests
{
    [Theory]
    [InlineData(1, 8, 0)]
    [InlineData(1, 7, 1)]
    [InlineData(1, 5, 2)]
    [InlineData(4, 4, 3)]
    [InlineData(5, 4, 4)]
    [InlineData(6, 2, 5)]
    [InlineData(7, 1, 6)]
    public void SevenTackleGradesMapToDistinctEffects(int first, int second, int expectedEffect)
    {
        var handler = new TackleHandler(
            new ActionResolver(new ScriptedActionRandom(first, second)));
        TackleResult result = handler.Resolve(new PlayerAttributes(), new PlayerAttributes());
        Assert.Equal(expectedEffect, (int)result.Effect);
    }

    [Fact]
    public void WrapCanStartAContinuousTackleStruggle()
    {
        var dice = new ScriptedActionRandom(4, 4, 6, 2);
        var handler = new TackleHandler(new ActionResolver(dice));
        var struggle = new TackleStruggle(handler, new PlayerAttributes(),
            new PlayerAttributes());
        Assert.Equal(TackleStruggleOutcome.Active, struggle.Update(0.49f));
        Assert.Equal(0, dice.Calls);
        Assert.Equal(TackleStruggleOutcome.Active, struggle.Update(0.01f));
        Assert.Equal(TackleEffect.Wrap, struggle.LastResult!.Effect);
        Assert.Equal(TackleStruggleOutcome.Tackled, struggle.Update(0.5f));
        Assert.Equal(4, dice.Calls);
    }

    [Fact]
    public void TacklerStartsAtTheFourthTackledFrameOnlyOnce()
    {
        var playback = new SpritePlayback(PlayerSpriteAssets.Layout, 10f);
        var cue = new TackleContactCue();
        playback.Play(0, loop: false, restart: true);
        cue.Arm();
        playback.Update(0.29f);
        Assert.Equal(2, playback.FrameIndex);
        Assert.False(cue.Consume(PlayerAnimationKind.Tackled, playback.FrameIndex));
        playback.Update(0.01f);
        Assert.Equal(3, playback.FrameIndex);
        Assert.True(cue.Consume(PlayerAnimationKind.Tackled, playback.FrameIndex));
        Assert.False(cue.Consume(PlayerAnimationKind.Tackled, playback.FrameIndex));
    }

    [Fact]
    public void DominantTackleNeedsASeparateBallSecurityFailureForFumble()
    {
        var carrier = new PlayerAttributes();
        var defender = new PlayerAttributes();
        var failed = new BallSecurityHandler(
            new ActionResolver(new ScriptedActionRandom(1, 8)));
        ActionResult fumble = failed.Resolve(carrier, defender);
        Assert.Equal(ActionOutcome.CriticalFailure, fumble.Outcome);
        Assert.True(BallSecurityHandler.IsFumble(fumble));

        var held = new BallSecurityHandler(
            new ActionResolver(new ScriptedActionRandom(4, 4)));
        Assert.False(BallSecurityHandler.IsFumble(held.Resolve(carrier, defender)));
    }
}
