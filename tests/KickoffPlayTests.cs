using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class KickoffPlayTests
{
    private static readonly Vector2 Kicker = new(160, 150);
    private static readonly Vector2 Returner = new(160, -82);

    [Fact]
    public void KickTravelsToReturnerThenCameraFollowsTheCarrier()
    {
        var play = new KickoffPlay(new ActionResolver(new ScriptedActionRandom(6, 1)));
        play.Begin(Kicker, Returner);
        for (int i = 0; i < 8; i++) play.Update(0.05f, Returner);
        Assert.Equal(KickoffPhase.Flight, play.Phase);

        for (int i = 0; i < 10; i++) play.Update(0.05f, Returner);
        Assert.InRange(play.BallFrame, 8, 15);
        Assert.True(play.BallPosition.Y < Kicker.Y);
        Assert.Equal(play.BallPosition, play.CameraTarget);

        for (int i = 0; i < 40; i++) play.Update(0.05f, Returner);
        Assert.Equal(KickoffPhase.Return, play.Phase);
        Vector2 moved = Returner + new Vector2(20, 25);
        play.Update(0.05f, moved);
        Assert.Equal(moved + new Vector2(2, -3), play.CameraTarget);
        play.EndReturn();
        Assert.Equal(KickoffPhase.Dead, play.Phase);
    }

    [Fact]
    public void MissedCatchBouncesAndCanBeRecovered()
    {
        var play = new KickoffPlay(new ActionResolver(new ScriptedActionRandom(6, 1)));
        play.Begin(Kicker, Returner);
        Vector2 displaced = Returner + new Vector2(20, 0);
        for (int i = 0; i < 60; i++) play.Update(0.05f, displaced);
        Assert.True(play.Phase is KickoffPhase.Bounce or KickoffPhase.Loose);
        Assert.InRange(play.BallFrame, 32, 39);
        play.Update(0.05f, Returner);
        Assert.Equal(KickoffPhase.Return, play.Phase);
    }

    [Theory]
    [InlineData(10f, 0f, 16)]
    [InlineData(0f, 20f, 24)]
    [InlineData(20f, 0f, 32)]
    public void MissDirectionSelectsNoseSideOrSkipBounce(float missX, float missY, int firstFrame)
    {
        var play = new KickoffPlay();
        play.Begin(Kicker, Returner);
        Vector2 displaced = Returner + new Vector2(missX, missY);
        for (int i = 0; i < 60 && play.Phase != KickoffPhase.Bounce; i++)
            play.Update(0.05f, displaced);
        Assert.Equal(KickoffPhase.Bounce, play.Phase);
        play.Update(0.01f, displaced);
        Assert.Equal(firstFrame, play.BallFrame);
    }

    [Fact]
    public void CriticalResultsShedOrPancake()
    {
        var shed = new BlockContest(4, 6, 5, 5,
            new ActionResolver(new ScriptedActionRandom(1, 10)));
        Assert.Equal(BlockOutcome.CoverageWins, shed.Update(0.5f,
            new Vector2(0, 8), new Vector2(0, 0), new Vector2(0, -20)));
        Assert.Equal(ActionOutcome.CriticalFailure, shed.LastResult!.Outcome);

        var pancake = new BlockContest(7, 5, 5, 5,
            new ActionResolver(new ScriptedActionRandom(10, 1)));
        Assert.Equal(BlockOutcome.BlockerWins, pancake.Update(0.5f,
            new Vector2(0, 8), new Vector2(0, 0), new Vector2(0, -20)));
        Assert.Equal(ActionOutcome.CriticalSuccess, pancake.LastResult!.Outcome);
    }

    [Fact]
    public void GradedPushMovesBothPlayersOverTheFollowingInterval()
    {
        var contest = new BlockContest(5, 5, 6, 5,
            new ActionResolver(new ScriptedActionRandom(8, 4, 5, 5)));
        Vector2 coverage = new(0, 8), blocker = new(0, 0), carrier = new(0, -20);
        Assert.Equal(BlockOutcome.Active, contest.Update(0.5f, coverage, blocker, carrier));
        Assert.Equal(ActionOutcome.MinorSuccess, contest.LastResult!.Outcome);
        Assert.Equal(Vector2.Zero, contest.MovementDelta);
        contest.Update(0.25f, coverage, blocker, carrier);
        Assert.InRange(contest.MovementDelta.Y, 0.3f, 0.7f);
    }

    [Fact]
    public void BlockChecksUseSimulationTimeAndDefenderWinsAtHoldLimit()
    {
        var largeRandom = new ScriptedActionRandom(5, 5, 5, 5);
        var smallRandom = new ScriptedActionRandom(5, 5, 5, 5);
        var large = new BlockContest(5, 5, 5, 5, new ActionResolver(largeRandom));
        var small = new BlockContest(5, 5, 5, 5, new ActionResolver(smallRandom));
        Vector2 coverage = new(0, 8), blocker = new(0, 0), carrier = new(0, -20);
        large.Update(1.25f, coverage, blocker, carrier);
        for (int i = 0; i < 25; i++) small.Update(0.05f, coverage, blocker, carrier);
        Assert.Equal(BlockOutcome.CoverageWins, large.Outcome);
        Assert.Equal(large.Outcome, small.Outcome);
        Assert.Equal(4, largeRandom.Calls);
        Assert.Equal(largeRandom.Calls, smallRandom.Calls);
    }

    [Fact]
    public void BlockReleasesOnSeparationOrCarrierPassing()
    {
        var separated = new BlockContest(5, 5, 5, 5,
            new ActionResolver(new ScriptedActionRandom()));
        Assert.Equal(BlockOutcome.Released, separated.Update(0.05f,
            new Vector2(0, 20), new Vector2(0, 0), new Vector2(0, -20)));
        var passed = new BlockContest(5, 5, 5, 5,
            new ActionResolver(new ScriptedActionRandom()));
        Assert.Equal(BlockOutcome.Released, passed.Update(0.05f,
            new Vector2(0, 8), new Vector2(0, 0), new Vector2(0, 23)));
    }

    [Fact]
    public void EngagementMotionClampsAtFieldEdgesAndOtherPlayers()
    {
        Vector2 shift = EngagementMotion.AllowedShift(new Vector2(0, 5),
            new Vector2(160, 8), new Vector2(160, 0), -170f, 350f, []);
        Assert.Equal(5f, shift.Y);
        Vector2 stopped = EngagementMotion.AllowedShift(new Vector2(0, 5),
            new Vector2(48, 8), new Vector2(48, 0), -170f, 350f,
            [new Vector2(48, 12)]);
        Assert.True(stopped.Y < 5f);
    }

    [Fact]
    public void TackleAndCatchUseDistanceBasedCollision()
    {
        Assert.True(KickoffPlay.Near(new Vector2(0, 0), new Vector2(8f, 0),
            BlockContest.TackleRadius));
        Assert.True(KickoffPlay.Near(new Vector2(0, 0), new Vector2(10.5f, 0),
            BlockContest.TackleRadius));
        Assert.False(KickoffPlay.Near(new Vector2(0, 0), new Vector2(12f, 0),
            BlockContest.TackleRadius));
        Assert.True(KickoffPlay.Near(Returner, Returner + new Vector2(0, 8),
            KickoffPlay.CatchRadius));
    }
}
