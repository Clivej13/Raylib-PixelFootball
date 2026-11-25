using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class PlayerCollisionTests
{
    private static readonly Vector2 Minimum = new(-200f, -200f);
    private static readonly Vector2 Maximum = new(200f, 200f);

    [Fact]
    public void FeetCircleStopsAtShoulderWidthEvenAcrossALargeStep()
    {
        Vector2 start = Vector2.Zero;
        Vector2 result = PlayerCollision.Move(start, new Vector2(20f, 0f),
            [new Vector2(10f, 0f)], Minimum, Maximum);
        Assert.InRange(result.X, 1.99f, 2.01f);
        Assert.True(PlayerCollision.Clear(result, [new Vector2(10f, 0f)]));
    }

    [Fact]
    public void FeetCircleSlidesAlongAnotherPlayer()
    {
        Vector2 obstacle = new(10f, 0f);
        Vector2 result = PlayerCollision.Move(Vector2.Zero, new Vector2(10f, 8f),
            [obstacle], Minimum, Maximum);
        Assert.True(result.Y > 0f);
        Assert.True(Vector2.Distance(result, obstacle) >= PlayerCollision.MinimumSeparation - 0.01f);
    }

    [Fact]
    public void ControlledAndAiPlayersStayApartDuringPursuitAndBlocking()
    {
        PlayerEntity[] coverage =
        [
            NewPlayer(100f, 80f, TeamId.Home),
            NewPlayer(120f, 80f, TeamId.Home)
        ];
        PlayerEntity[] blockers =
        [
            NewPlayer(100f, 60f, TeamId.Away),
            NewPlayer(120f, 60f, TeamId.Away)
        ];
        PlayerEntity runner = NewPlayer(110f, 40f, TeamId.Away);
        PlayerEntity[] field = [.. coverage, .. blockers, runner];
        var blocking = new KickoffBlocking(coverage, blockers,
            new ActionResolver(new SeededActionRandom(42)));

        for (int frame = 0; frame < 240; frame++)
        {
            runner.Update(new Vector2(0f, 1f), 0.05f, -170f, 350f, field);
            blocking.Update(0.05f, runner.Position, -170f, 350f, field);
            for (int first = 0; first < field.Length; first++)
                for (int second = first + 1; second < field.Length; second++)
                    if (!field[first].IsDowned && !field[second].IsDowned)
                    Assert.True(Vector2.Distance(field[first].Position, field[second].Position)
                        >= PlayerCollision.MinimumSeparation - 0.02f,
                        $"Players {first} and {second} overlapped at frame {frame}.");
        }
    }

    [Fact]
    public void FullKickoffFieldNeverStacksPlayersAtTheReturner()
    {
        PlayerEntity[] field = Enumerable.Range(0, 22).Select(index =>
        {
            int column = index % 11;
            float y = index < 11 ? 145f : -28f;
            if (index == 5) y = 150f;
            if (index == 16) y = -82f;
            return NewPlayer(65f + column * 19f, y,
                index < 11 ? TeamId.Home : TeamId.Away);
        }).ToArray();
        PlayerEntity runner = field[16];
        var blocking = new KickoffBlocking(field.Take(11).ToArray(),
            field.Skip(11).Where(player => !ReferenceEquals(player, runner)).ToArray(),
            new ActionResolver(new SeededActionRandom(19)));
        for (int frame = 0; frame < 400; frame++)
        {
            blocking.Update(0.05f, runner.Position, -170f, 350f, field);
            for (int first = 0; first < field.Length; first++)
                for (int second = first + 1; second < field.Length; second++)
                    if (!field[first].IsDowned && !field[second].IsDowned)
                    Assert.True(Vector2.Distance(field[first].Position, field[second].Position)
                        >= PlayerCollision.MinimumSeparation - 0.02f,
                        $"Players {first} and {second} overlapped at frame {frame}.");
        }
    }

    private static PlayerEntity NewPlayer(float x, float y, TeamId team) =>
        new(null!, new PlayerSpawn(new Vector2(x, y), 0, 0f, team),
            PlayerAnimationKind.Run);
}
