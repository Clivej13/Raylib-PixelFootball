using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class BreakTackleAnimationTests
{
    [Fact]
    public void FailedTackleOutcomesSelectTheCarrierAnimation()
    {
        Assert.Equal(PlayerAnimationKind.Juke,
            BreakTackleAnimations.ForContact(TackleEffect.Miss));
        Assert.Equal(PlayerAnimationKind.Truck,
            BreakTackleAnimations.ForContact(TackleEffect.Broken));
        Assert.Equal(PlayerAnimationKind.StiffArm,
            BreakTackleAnimations.ForContact(TackleEffect.Glance));
        Assert.Equal(PlayerAnimationKind.Spin,
            BreakTackleAnimations.ForEscapedWrap());
        Assert.Null(BreakTackleAnimations.ForContact(TackleEffect.Wrap));
        Assert.Null(BreakTackleAnimations.ForContact(TackleEffect.Complete));
    }

    [Fact]
    public void BallCarrierKeepsMovingDuringEachBreakTackleAnimation()
    {
        foreach (PlayerAnimationKind action in new[]
        {
            PlayerAnimationKind.Juke, PlayerAnimationKind.Spin,
            PlayerAnimationKind.Truck, PlayerAnimationKind.StiffArm
        })
        {
            var carrier = new PlayerEntity(null!,
                new PlayerSpawn(new Vector2(100f, 100f), 4, 0f, TeamId.Away),
                PlayerAnimationKind.Run);
            carrier.SetBallCarrier(true);
            carrier.StartAction(action);
            carrier.Update(Vector2.UnitY, 0.2f, -170f, 350f);
            Assert.Equal(action, carrier.Animation);
            Assert.True(carrier.Position.Y > 100f);
            carrier.Update(Vector2.UnitY, 0.7f, -170f, 350f);
            Assert.Equal(PlayerAnimationKind.RunWithBall, carrier.Animation);
            Assert.True(carrier.HasBall);
        }
    }
}
