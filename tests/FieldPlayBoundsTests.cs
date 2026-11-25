using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class FieldPlayBoundsTests
{
    private const float Top = -170f;
    private const float Bottom = 350f;

    [Theory]
    [InlineData(48f, 0f, 1)]
    [InlineData(272f, 0f, 1)]
    [InlineData(47f, 0f, 1)]
    [InlineData(273f, 0f, 1)]
    [InlineData(160f, -113f, 2)]
    [InlineData(160f, 293f, 2)]
    [InlineData(160f, -112f, 0)]
    [InlineData(160f, 292f, 0)]
    public void CarrierCrossingsHaveAPlayEndingReason(
        float x, float y, int expected) =>
        Assert.Equal((KickoffEndReason)expected,
            FieldPlayBounds.CheckCarrier(new Vector2(x, y), Top, Bottom));

    [Fact]
    public void AClampedCarrierCanStillReachTheSidelineEndCondition()
    {
        var carrier = new PlayerEntity(null!,
            new PlayerSpawn(new Vector2(65f, 0f), 6, 0f, TeamId.Away));
        carrier.Update(-Vector2.UnitX, 1f, Top, Bottom);
        Assert.Equal(FieldPlayBounds.LeftSideline, carrier.Position.X);
        Assert.Equal(KickoffEndReason.OutOfBounds,
            FieldPlayBounds.CheckCarrier(carrier.Position, Top, Bottom));
    }

    [Theory]
    [InlineData(1, "OUT OF BOUNDS")]
    [InlineData(2, "END ZONE")]
    [InlineData(3, "TACKLED")]
    public void EndedReturnBecomesDeadAndStopsUpdating(
        int reasonValue, string status)
    {
        KickoffEndReason reason = (KickoffEndReason)reasonValue;
        var play = ReadyReturn();
        play.EndReturn(reason);
        Vector2 stopped = play.BallPosition;
        play.Update(1f, stopped + new Vector2(50f, 50f));
        Assert.Equal(KickoffPhase.Dead, play.Phase);
        Assert.Equal(reason, play.EndReason);
        Assert.Equal(BallPossessionState.DeadBall, play.Ball.State);
        Assert.Equal(stopped, play.BallPosition);
        Assert.Contains(status, play.Status);
    }

    [Fact]
    public void NewKickoffClearsThePreviousEndReason()
    {
        var play = ReadyReturn();
        play.EndReturn(KickoffEndReason.OutOfBounds);
        play.Begin(new Vector2(160f, 150f), new Vector2(160f, -82f));
        Assert.Equal(KickoffPhase.Windup, play.Phase);
        Assert.Equal(KickoffEndReason.None, play.EndReason);
    }

    [Fact]
    public void DominantTackleFumbleCanBeRecordedWithoutKeepingThePlayLive()
    {
        var play = ReadyReturn();
        play.FumbleAt(new Vector2(160f, 12f));
        Assert.Equal(KickoffPhase.Loose, play.Phase);
        play.EndReturn(KickoffEndReason.Tackled);
        Assert.Equal(KickoffPhase.Dead, play.Phase);
        Assert.Equal(KickoffEndReason.Tackled, play.EndReason);
        Assert.Equal(LooseBallOrigin.Fumble, play.Ball.LooseOrigin);
    }

    private static KickoffPlay ReadyReturn()
    {
        Vector2 returner = new(160f, -82f);
        var play = new KickoffPlay(new ActionResolver(new ScriptedActionRandom(6, 1)));
        play.Begin(new Vector2(160f, 150f), returner);
        play.Update(2.7f, returner);
        Assert.Equal(KickoffPhase.Return, play.Phase);
        return play;
    }
}
