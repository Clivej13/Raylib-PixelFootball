using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class TackleContactTests
{
    [Fact]
    public void FailedContactCanBeRetriedAfterSimulationTime()
    {
        PlayerEntity defender = NewPlayer(100f, 100f, TeamId.Home);
        var gate = new TackleContactGate();
        gate.Mark(defender);
        gate.Advance(0.25f);
        Assert.Contains(defender, gate.Excluded);
        gate.Advance(0.24f);
        Assert.Contains(defender, gate.Excluded);
        gate.Advance(0.01f);
        Assert.DoesNotContain(defender, gate.Excluded);
    }

    [Fact]
    public void CarrierContactCanInterruptABlockForATackle()
    {
        PlayerEntity defender = NewPlayer(100f, 100f, TeamId.Home);
        PlayerEntity blocker = NewPlayer(100f, 108f, TeamId.Away);
        PlayerEntity[] field = [defender, blocker];
        var blocking = new KickoffBlocking([defender], [blocker]);
        Vector2 carrier = new(100f, 100f);

        blocking.Update(0.05f, carrier, -170f, 350f, field);
        Assert.Equal(1, blocking.ActiveCount);
        Assert.Null(blocking.GetTacklerAt(carrier));
        Assert.Same(defender, blocking.TakeTacklerAt(carrier));
        Assert.Equal(0, blocking.ActiveCount);
        Assert.Equal(PlayerAnimationKind.Run, defender.Animation);
    }

    [Fact]
    public void FallenMissedTacklerDoesNotBlockTheNextDefender()
    {
        PlayerEntity missed = NewPlayer(100f, 100f, TeamId.Home);
        PlayerEntity next = NewPlayer(100f, 120f, TeamId.Home);
        PlayerEntity carrier = NewPlayer(100f, 80f, TeamId.Away);
        PlayerEntity[] field = [missed, next, carrier];
        missed.KnockDown(DownEvent.MissedTackle, 0.4f,
            PlayerAnimationKind.BlockLoss);

        Assert.False(missed.BlocksMovement);
        next.Update(-Vector2.UnitY, 1f, -170f, 350f, field);
        Assert.True(next.Position.Y < missed.Position.Y);
        Assert.True(KickoffPlay.Near(next.Position, carrier.Position,
            BlockContest.TackleRadius));
        var blocking = new KickoffBlocking([missed, next],
            [NewPlayer(160f, 160f, TeamId.Away)]);
        Assert.Same(next, blocking.TakeTacklerAt(carrier.Position));
        Assert.Equal(PlayerAnimationKind.BlockLoss, missed.Animation);
        missed.AnimateInPlace(0.9f, field);
        Assert.Equal(PlayerAnimationKind.GetUpFast, missed.Animation);
    }

    private static PlayerEntity NewPlayer(float x, float y, TeamId team) =>
        new(null!, new PlayerSpawn(new Vector2(x, y), 0, 0f, team),
            PlayerAnimationKind.Run);
}
