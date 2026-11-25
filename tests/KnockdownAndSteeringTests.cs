using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class KnockdownAndSteeringTests
{
    [Fact]
    public void PlayerStaysDownUntilRecoveryAndTheWinnerHasWalkedClear()
    {
        PlayerEntity loser = NewPlayer(100f, 100f, TeamId.Away);
        PlayerEntity winner = NewPlayer(100f, 92f, TeamId.Home);
        PlayerEntity[] field = [loser, winner];
        loser.KnockDown(DownEvent.LostBlock, 0.85f);
        winner.MoveEngaged(new Vector2(0f, 8f), -170f, 350f);

        loser.Update(Vector2.UnitY, 1f, -170f, 350f, field);
        Assert.True(loser.IsDowned);
        Assert.Equal(new Vector2(100f, 100f), loser.Position);
        Assert.Equal(PlayerAnimationKind.LyingDown, loser.Animation);

        winner.MoveEngaged(new Vector2(0f, 10f), -170f, 350f);
        loser.Update(Vector2.Zero, 0.05f, -170f, 350f, field);
        Assert.True(loser.IsDowned);
        Assert.Equal(PlayerAnimationKind.GetUpSlow, loser.Animation);
        loser.Update(Vector2.Zero, 1f, -170f, 350f, field);
        Assert.False(loser.IsDowned);
        Assert.Equal(PlayerAnimationKind.Idle, loser.Animation);
    }

    [Fact]
    public void DefenderCanRunOverADownedBlocker()
    {
        PlayerEntity blocker = NewPlayer(100f, 100f, TeamId.Away);
        PlayerEntity defender = NewPlayer(100f, 92f, TeamId.Home);
        PlayerEntity[] field = [blocker, defender];
        blocker.KnockDown(DownEvent.LostBlock, 1.2f);

        defender.Update(Vector2.UnitY, 0.5f, -170f, 350f, field);
        Assert.True(defender.Position.Y > blocker.Position.Y);
        Assert.True(blocker.IsDowned);
    }

    [Fact]
    public void DefenderRoutesAroundStandingBlocker()
    {
        PlayerEntity defender = NewPlayer(100f, 120f, TeamId.Home);
        PlayerEntity blocker = NewPlayer(100f, 100f, TeamId.Away);
        PlayerEntity[] field = [defender, blocker];
        var coverage = new CoverageAgent(defender, [blocker]);
        Vector2 target = new(100f, 60f);
        Vector2 waypoint = PlayerSteering.AroundBlockers(defender.Position, target, [blocker]);
        Assert.True(MathF.Abs(waypoint.X - blocker.Position.X)
            >= PlayerSteering.DetourClearance - 0.01f);

        for (int i = 0; i < 80; i++)
            coverage.Update(0.05f, target, null, -170f, 350f, field);
        Assert.True(defender.Position.Y < 95f);
        Assert.True(Vector2.Distance(defender.Position, blocker.Position)
            >= PlayerCollision.MinimumSeparation - 0.01f);
    }

    [Fact]
    public void BlockersStayInTheirOwnLanesWhileApproachingAThreat()
    {
        PlayerEntity left = NewPlayer(100f, 120f, TeamId.Away);
        PlayerEntity right = NewPlayer(119f, 120f, TeamId.Away);
        PlayerEntity threat = NewPlayer(109f, 70f, TeamId.Home);
        PlayerEntity[] field = [left, right, threat];
        var leftAgent = new BlockerAgent(left);
        var rightAgent = new BlockerAgent(right);
        var coverage = new CoverageAgent(threat);

        for (int i = 0; i < 70; i++)
        {
            leftAgent.Update(0.05f, threat.Position, coverage, -170f, 350f, field);
            rightAgent.Update(0.05f, threat.Position, coverage, -170f, 350f, field);
        }
        Assert.InRange(left.Position.X, 100f, 100f + BlockerAgent.LaneTravel + 0.01f);
        Assert.InRange(right.Position.X, 119f - BlockerAgent.LaneTravel - 0.01f, 119f);
        Assert.True(Vector2.Distance(left.Position, right.Position)
            >= PlayerCollision.MinimumSeparation - 0.01f);
    }

    private static PlayerEntity NewPlayer(float x, float y, TeamId team) =>
        new(null!, new PlayerSpawn(new Vector2(x, y), 0, 0f, team),
            PlayerAnimationKind.Run);
}
