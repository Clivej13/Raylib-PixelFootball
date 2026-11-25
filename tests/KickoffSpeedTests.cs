using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class KickoffSpeedTests
{
    [Fact]
    public void UnobstructedReturnerCoverageAndBlockerShareBaseSpeed()
    {
        PlayerEntity runner = NewPlayer(100f);
        PlayerEntity defender = NewPlayer(120f);
        PlayerEntity blocker = NewPlayer(140f);
        var coverage = new CoverageAgent(defender);
        var blocking = new BlockerAgent(blocker);
        const float elapsed = 0.05f;

        runner.Update(Vector2.UnitY, elapsed, -170f, 350f, [runner]);
        coverage.Update(elapsed, new Vector2(200f, 200f), null,
            -170f, 350f, [defender]);
        blocking.Update(elapsed, new Vector2(200f, 200f),
            new CoverageAgent(NewPlayer(200f)),
            -170f, 350f, [blocker]);

        float expected = PlayerEntity.TopSpeedWorldUnitsPerSecond * elapsed;
        Assert.InRange(Vector2.Distance(runner.Position, new Vector2(100f, 100f)),
            expected - 0.001f, expected + 0.001f);
        Assert.InRange(Vector2.Distance(defender.Position, new Vector2(120f, 100f)),
            expected - 0.001f, expected + 0.001f);
        Assert.InRange(Vector2.Distance(blocker.Position, new Vector2(140f, 100f)),
            expected - 0.001f, expected + 0.001f);
    }

    private static PlayerEntity NewPlayer(float x) =>
        new(null!, new PlayerSpawn(new Vector2(x, 100f), 4, 0f, TeamId.Home),
            PlayerAnimationKind.Run);
}
