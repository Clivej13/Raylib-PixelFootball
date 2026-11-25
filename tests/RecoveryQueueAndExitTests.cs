using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class RecoveryQueueAndExitTests
{
    [Fact]
    public void ThreePlayerPileGetsUpFromTopToBottomOnlyAfterEachMovesClear()
    {
        PlayerEntity bottom = NewPlayer(TeamId.Away);
        PlayerEntity middle = NewPlayer(TeamId.Home);
        PlayerEntity top = NewPlayer(TeamId.Home);
        PlayerEntity[] field = [bottom, middle, top];
        var queue = new PileRecoveryQueue();
        queue.Register(field);
        foreach (PlayerEntity player in field)
        {
            player.JoinRecoveryQueue(queue);
            player.KnockDown(DownEvent.MadeTackle, 0.2f);
        }
        Assert.Equal(2, queue.Depth(top));
        Assert.False(queue.CanGetUp(bottom));

        top.AnimateInPlace(2f, field);
        middle.AnimateInPlace(2f, field);
        bottom.AnimateInPlace(2f, field);
        Assert.False(top.IsDowned);
        Assert.True(middle.IsDowned);
        Assert.True(bottom.IsDowned);

        top.MoveEngaged(new Vector2(20f, 0f), -170f, 350f);
        middle.AnimateInPlace(2f, field);
        bottom.AnimateInPlace(2f, field);
        Assert.False(middle.IsDowned);
        Assert.True(bottom.IsDowned);

        middle.MoveEngaged(new Vector2(-20f, 0f), -170f, 350f);
        bottom.AnimateInPlace(2f, field);
        Assert.False(bottom.IsDowned);
    }

    [Fact]
    public void EmptyNextPlayRostersSendAllTwentyTwoPlayersToTheirOwnSidelines()
    {
        PlayerEntity[] field = FieldPopulation.CreateField()
            .Select(spawn => new PlayerEntity(null!, spawn, PlayerAnimationKind.Run))
            .ToArray();
        var exits = new PostPlayExit();
        exits.Begin(field, [], [], -170f, 350f);
        Assert.Equal(22, exits.RemainingCount);
        for (int frame = 0; frame < 800 && exits.RemainingCount > 0; frame++)
            exits.Update(0.05f, -170f, 350f, field);

        Assert.Equal(0, exits.RemainingCount);
        Assert.All(field, player => Assert.True(exits.HasExited(player)));
        Assert.All(field.Where(player => player.Team == TeamId.Home),
            player => Assert.True(player.Position.X <= 32f));
        Assert.All(field.Where(player => player.Team == TeamId.Away),
            player => Assert.True(player.Position.X >= 288f));
    }

    [Fact]
    public void TacklerWalksClearBeforeCarrierCanRiseAndBothExit()
    {
        PlayerEntity carrier = NewPlayer(TeamId.Away);
        PlayerEntity tackler = NewPlayer(TeamId.Home);
        PlayerEntity[] field = [carrier, tackler];
        var queue = new PileRecoveryQueue();
        queue.Register(field);
        foreach (PlayerEntity player in field) player.JoinRecoveryQueue(queue);
        carrier.KnockDown(DownEvent.Tackled, 1.1f);
        tackler.KnockDown(DownEvent.MadeTackle, 0.2f);
        var exits = new PostPlayExit();
        exits.Begin(field, [], [], -170f, 350f);

        bool carrierRose = false;
        for (int frame = 0; frame < 500 && exits.RemainingCount > 0; frame++)
        {
            exits.Update(0.05f, -170f, 350f, field);
            if (!carrier.IsDowned && !carrierRose)
            {
                carrierRose = true;
                Assert.False(tackler.IsDowned);
                Assert.True(Vector2.Distance(carrier.Position, tackler.Position)
                    >= PlayerCollision.MinimumSeparation);
            }
        }
        Assert.True(carrierRose);
        Assert.Equal(0, exits.RemainingCount);
    }

    [Fact]
    public void NextPlaySelectionKeepsSelectedPlayersOnTheField()
    {
        PlayerEntity home = NewPlayer(TeamId.Home);
        PlayerEntity away = NewPlayer(TeamId.Away);
        var exits = new PostPlayExit();
        exits.Begin([home, away], [home], [away], -170f, 350f);
        Assert.Equal(0, exits.RemainingCount);
        exits.Reset();
        Assert.False(exits.Active);
    }

    private static PlayerEntity NewPlayer(TeamId team) =>
        new(null!, new PlayerSpawn(new Vector2(100f, 100f), 0, 0f, team),
            PlayerAnimationKind.Run);
}
