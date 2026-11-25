using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class RecoveryAnimationTests
{
    [Fact]
    public void DownEventSelectsTheExpectedRecovery()
    {
        Assert.Equal(PlayerAnimationKind.GetUpHurt,
            RecoveryAnimations.For(DownEvent.Tackled).GetUpAnimation);
        Assert.Equal(PlayerAnimationKind.GetUpFast,
            RecoveryAnimations.For(DownEvent.MadeTackle).GetUpAnimation);
        Assert.Equal(PlayerAnimationKind.GetUpFast,
            RecoveryAnimations.For(DownEvent.MissedTackle).GetUpAnimation);
        Assert.Equal(PlayerAnimationKind.GetUpSlow,
            RecoveryAnimations.For(DownEvent.LostBlock).GetUpAnimation);
        Assert.Equal(PlayerAnimationKind.GetUpSlow,
            RecoveryAnimations.For(DownEvent.LostPassRush).GetUpAnimation);
    }

    [Fact]
    public void TackledCarrierFallsLiesDownThenGetsUpHurt()
    {
        PlayerEntity carrier = NewPlayer(TeamId.Away);
        carrier.KnockDown(DownEvent.Tackled, 1.1f);

        carrier.AnimateInPlace(0.4f);
        Assert.Equal(PlayerAnimationKind.Tackled, carrier.Animation);
        carrier.AnimateInPlace(0.4f);
        Assert.Equal(PlayerAnimationKind.LyingDown, carrier.Animation);
        carrier.AnimateInPlace(0.35f);
        Assert.Equal(PlayerAnimationKind.GetUpHurt, carrier.Animation);
        Assert.True(carrier.IsDowned);
        carrier.AnimateInPlace(1.2f);
        Assert.False(carrier.IsDowned);
        Assert.Equal(PlayerAnimationKind.Run, carrier.Animation);
    }

    [Fact]
    public void SuccessfulTacklerPlaysTackleThenFastRecovery()
    {
        PlayerEntity tackler = NewPlayer(TeamId.Home);
        tackler.BeginTackleRecovery();
        tackler.AnimateInPlace(0.8f);
        Assert.True(tackler.IsDowned);
        Assert.Equal(PlayerAnimationKind.LyingDown, tackler.Animation);
        tackler.AnimateInPlace(0.25f);
        Assert.Equal(PlayerAnimationKind.GetUpFast, tackler.Animation);
        tackler.AnimateInPlace(0.65f);
        Assert.False(tackler.IsDowned);
        Assert.Equal(PlayerAnimationKind.Run, tackler.Animation);
    }

    [Fact]
    public void BlockContestLosersGetUpSlowly()
    {
        foreach (DownEvent cause in new[] { DownEvent.LostBlock, DownEvent.LostPassRush })
        {
            PlayerEntity loser = NewPlayer(TeamId.Away);
            loser.KnockDown(cause, 0.85f);
            loser.AnimateInPlace(0.9f);
            Assert.Equal(PlayerAnimationKind.GetUpSlow, loser.Animation);
            loser.AnimateInPlace(1f);
            Assert.False(loser.IsDowned);
        }
    }

    [Fact]
    public void OccupiedFeetDelayAndInterruptGetUp()
    {
        PlayerEntity loser = NewPlayer(TeamId.Away);
        PlayerEntity winner = NewPlayer(TeamId.Home);
        PlayerEntity[] field = [loser, winner];
        loser.KnockDown(DownEvent.LostBlock, 0.85f);
        loser.AnimateInPlace(1.5f, field);
        Assert.Equal(PlayerAnimationKind.LyingDown, loser.Animation);
        Assert.True(loser.IsDowned);

        winner.MoveEngaged(new Vector2(20f, 0f), -170f, 350f);
        loser.AnimateInPlace(0.05f, field);
        Assert.Equal(PlayerAnimationKind.GetUpSlow, loser.Animation);

        winner.MoveEngaged(new Vector2(-20f, 0f), -170f, 350f);
        loser.AnimateInPlace(0.05f, field);
        Assert.Equal(PlayerAnimationKind.LyingDown, loser.Animation);
        Assert.True(loser.IsDowned);

        winner.MoveEngaged(new Vector2(20f, 0f), -170f, 350f);
        loser.AnimateInPlace(1.1f, field);
        Assert.False(loser.IsDowned);
    }

    private static PlayerEntity NewPlayer(TeamId team) =>
        new(null!, new PlayerSpawn(new Vector2(100f, 100f), 0, 0f, team),
            PlayerAnimationKind.Run);
}
