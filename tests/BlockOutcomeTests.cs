using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class BlockOutcomeTests
{
    [Fact]
    public void SkillAndCheckResultsChangeHowLongABlockCanHold()
    {
        var resolver = new ActionResolver(new ScriptedActionRandom());
        var stronger = new BlockContest(8, 3, 5, 5, resolver);
        var weaker = new BlockContest(3, 8, 5, 5, resolver);
        Assert.True(stronger.HoldDuration > ActionProfiles.Blocking.MaximumDuration);
        Assert.True(weaker.HoldDuration < ActionProfiles.Blocking.MaximumDuration);

        var lostGround = new BlockContest(5, 5, 5, 5,
            new ActionResolver(new ScriptedActionRandom(1, 6)));
        var gainedGround = new BlockContest(5, 5, 5, 5,
            new ActionResolver(new ScriptedActionRandom(6, 1)));
        Vector2 defender = new(0f, 8f), blocker = Vector2.Zero, carrier = new(0f, -20f);
        lostGround.Update(0.5f, defender, blocker, carrier);
        gainedGround.Update(0.5f, defender, blocker, carrier);
        Assert.True(lostGround.HoldDuration < gainedGround.HoldDuration);
    }

    [Fact]
    public void ExpiredBlockTriggersRushWinAndCannotRestartWithSamePair()
    {
        var dice = new ScriptedActionRandom(Enumerable.Repeat(5, 40).ToArray());
        (KickoffBlocking blocking, PlayerEntity defender, PlayerEntity blocker,
            PlayerEntity[] field, Vector2 carrier) = Pair(new ActionResolver(dice));
        for (int i = 0; i < 27; i++)
            blocking.Update(0.05f, carrier, -170f, 350f, field);
        Assert.Equal(0, blocking.ActiveCount);
        Assert.Equal(PlayerAnimationKind.BullRush, defender.Animation);
        Assert.Equal(PlayerAnimationKind.Tackled, blocker.Animation);
        Assert.True(blocker.IsDowned);
        int calls = dice.Calls;
        for (int i = 0; i < 80; i++)
            blocking.Update(0.05f, carrier, -170f, 350f, field);
        Assert.Equal(0, blocking.ActiveCount);
        Assert.Equal(calls, dice.Calls);
    }

    [Fact]
    public void CriticalBlockerWinStillPancakesDefender()
    {
        var dice = new ScriptedActionRandom(10, 1);
        (KickoffBlocking blocking, PlayerEntity defender, PlayerEntity blocker,
            PlayerEntity[] field, Vector2 carrier) = Pair(new ActionResolver(dice));
        for (int i = 0; i < 12; i++)
            blocking.Update(0.05f, carrier, -170f, 350f, field);
        Assert.Equal(0, blocking.ActiveCount);
        Assert.Equal(ActionOutcome.CriticalSuccess, blocking.LastResolution!.Outcome);
        Assert.Equal(PlayerAnimationKind.Tackled, defender.Animation);
        Assert.True(defender.IsDowned);
        Assert.Equal(PlayerAnimationKind.BullRush, blocker.Animation);
    }

    private static (KickoffBlocking, PlayerEntity, PlayerEntity, PlayerEntity[], Vector2)
        Pair(ActionResolver resolver)
    {
        var defender = new PlayerEntity(null!,
            new PlayerSpawn(new Vector2(100f, 108f), 0, 0f, TeamId.Home));
        var blocker = new PlayerEntity(null!,
            new PlayerSpawn(new Vector2(100f, 100f), 4, 0f, TeamId.Away));
        PlayerEntity[] field = [defender, blocker];
        return (new KickoffBlocking([defender], [blocker], resolver),
            defender, blocker, field, new Vector2(100f, 80f));
    }
}
