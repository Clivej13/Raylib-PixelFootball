using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class BallPossessionTests
{
    private static readonly Vector2 Spot = new(160, -82);

    [Theory]
    [InlineData(-3, 1)]
    [InlineData(-2, 0)]
    [InlineData(-1, 2)]
    [InlineData(0, 3)]
    [InlineData(1, 4)]
    [InlineData(2, 4)]
    [InlineData(3, 5)]
    public void SevenCatchGradesCreateDistinctPhysicalStates(int grade, int expectedState)
    {
        var ball = new BallPossession();
        ball.BeginFlight(Spot, BallPlayType.Kickoff);
        ActionOutcome outcome = (ActionOutcome)grade;
        ball.ApplyCatch(Result(outcome), ActionProfiles.Catching.ConsequenceFor(outcome), 7, Spot);
        Assert.Equal(expectedState, (int)ball.State);
        Assert.Equal(1, ball.CatchAttempts);
        Assert.Equal(grade >= 0, ball.WasPossessed);
    }

    [Fact]
    public void CriticalDeflectionRemainsAirborneAndCanBeCaughtAgain()
    {
        var ball = new BallPossession();
        ball.BeginFlight(Spot, BallPlayType.Pass);
        ball.ApplyCatch(Result(ActionOutcome.CriticalFailure),
            ActionProfiles.Catching.ConsequenceFor(ActionOutcome.CriticalFailure), 7, Spot);
        ball.Update(0.1f, null, Spot.Y);
        Assert.True(ball.Position.Y < Spot.Y);
        Assert.False(ball.CanAttemptCatch);
        ball.Update(0.1f, null, Spot.Y);
        Assert.True(ball.CanAttemptCatch);
        ball.ApplyCatch(Result(ActionOutcome.CriticalSuccess),
            ActionProfiles.Catching.ConsequenceFor(ActionOutcome.CriticalSuccess), 8, ball.Position);
        Assert.Equal(BallPossessionState.SecuredPossession, ball.State);
        Assert.Equal(8, ball.CarrierId);
    }

    [Fact]
    public void BobbleAttemptsAreBoundedAndEventuallyBecomeLoose()
    {
        var ball = new BallPossession();
        ball.BeginFlight(Spot, BallPlayType.Kickoff);
        for (int attempt = 0; attempt < BallPossession.MaximumCatchAttempts; attempt++)
        {
            ball.ApplyCatch(Result(ActionOutcome.MinorFailure),
                ActionProfiles.Catching.ConsequenceFor(ActionOutcome.MinorFailure), 7, Spot);
            ball.Update(0.8f, null, Spot.Y);
            if (attempt + 1 < BallPossession.MaximumCatchAttempts)
                Assert.True(ball.CanAttemptCatch);
        }
        Assert.Equal(BallPossessionState.LooseBall, ball.State);
        Assert.False(ball.CanAttemptCatch);
    }

    [Fact]
    public void PassDropIsIncompleteOnlyBeforePossession()
    {
        var incomplete = new BallPossession();
        incomplete.BeginFlight(Spot, BallPlayType.Pass);
        incomplete.ApplyCatch(Result(ActionOutcome.MajorFailure),
            ActionProfiles.Catching.ConsequenceFor(ActionOutcome.MajorFailure), 1, Spot);
        incomplete.Update(0.05f, null, Spot.Y);
        Assert.Equal(BallPossessionState.DeadBall, incomplete.State);
        Assert.Equal(BallDeadReason.IncompletePass, incomplete.DeadReason);

        var fumble = new BallPossession();
        fumble.BeginFlight(Spot, BallPlayType.Pass);
        fumble.ApplyCatch(Result(ActionOutcome.Neutral),
            ActionProfiles.Catching.ConsequenceFor(ActionOutcome.Neutral), 1, Spot);
        fumble.LosePossession(Spot);
        Assert.Equal(BallPossessionState.LooseBall, fumble.State);
        Assert.Equal(LooseBallOrigin.Fumble, fumble.LooseOrigin);

        var muff = new BallPossession();
        muff.BeginFlight(Spot, BallPlayType.Kickoff);
        muff.LandAt(Spot);
        Assert.Equal(LooseBallOrigin.MuffedKick, muff.LooseOrigin);
    }

    [Fact]
    public void CatchQualitySetsRecoveryAndMomentum()
    {
        var ball = new BallPossession();
        ball.BeginFlight(Spot, BallPlayType.Kickoff);
        ActionConsequence consequence = ActionProfiles.Catching.ConsequenceFor(ActionOutcome.MinorSuccess);
        ball.ApplyCatch(Result(ActionOutcome.MinorSuccess), consequence, 7, Spot);
        Assert.Equal(0.3f, ball.RecoveryRemaining);
        Assert.Equal(0.65f, ball.MomentumRetention);
        ball.Update(0.3f, Spot + new Vector2(3, 0), Spot.Y);
        Assert.Equal(BallPossessionState.SecuredPossession, ball.State);
        Assert.Equal(1f, ball.MomentumRetention);
    }

    [Fact]
    public void CatchFactorsAreRecordedByTheSharedResolver()
    {
        var handler = new CatchHandler(new ActionResolver(new ScriptedActionRandom(3, 5)));
        var ball = new BallPossession();
        ball.BeginFlight(Spot, BallPlayType.Kickoff);
        ActionResult result = handler.Attempt(ball, new PlayerAttributes(
            new Dictionary<PlayerAttribute, int> { [PlayerAttribute.Catching] = 7 }),
            new CatchFactors(PassAccuracy: 1, BallVelocity: -1,
                DefensiveCoverage: -2, DefensiveContact: -1, ReceiverMovement: -1,
                BodyPosition: 1, CatchRadius: -1, Fatigue: -1), 7, Spot);
        Assert.Equal(8, result.AppliedModifiers.Count);
        Assert.Equal(-5, result.ModifierTotal);
        Assert.Equal(-4f, result.Score);
        Assert.Equal(BallPossessionState.InFlight, ball.State);
    }

    private static ActionResult Result(ActionOutcome outcome) =>
        new(outcome, (int)outcome, 7, 4, 3, 3, []);
}
