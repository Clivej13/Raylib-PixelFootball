using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class ActionResolutionTests
{
    [Theory]
    [InlineData(-7, -3)]
    [InlineData(-6, -3)]
    [InlineData(-5, -2)]
    [InlineData(-4, -2)]
    [InlineData(-3, -1)]
    [InlineData(-2, -1)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(6, 3)]
    public void DefaultThresholdsClassifyEveryBoundary(int score, int expected)
    {
        Assert.Equal(expected, (int)new ActionThresholds().Classify(score));
    }

    [Fact]
    public void ResultContainsSkillsRollsAndNamedModifiers()
    {
        var resolver = new ActionResolver(new ScriptedActionRandom(6, 2));
        ActionResult result = resolver.Resolve(new ActionContext(7, 5,
            [new ActionModifier("Body position", 1), new ActionModifier("Contact", -2)]),
            new ActionConfig());
        Assert.Equal(5f, result.Score);
        Assert.Equal(ActionOutcome.MajorSuccess, result.Outcome);
        Assert.Equal(7, result.PrimarySkill);
        Assert.Equal(5, result.OpposingSkill);
        Assert.Equal(6, result.PrimaryRoll);
        Assert.Equal(2, result.OpposingRoll);
        Assert.Equal(-1, result.ModifierTotal);
        Assert.Equal(2, result.AppliedModifiers.Count);
    }

    [Fact]
    public void InvalidRatingsProfilesAndRollsAreRejected()
    {
        var resolver = new ActionResolver(new ScriptedActionRandom(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            resolver.Resolve(new ActionContext(0), new ActionConfig()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            resolver.Resolve(new ActionContext(5, 11), new ActionConfig()));
        Assert.Throws<ArgumentException>(() =>
            new ActionConfig(thresholds: new ActionThresholds(-6, -4, -4, 1, 3, 5)));
        Assert.Throws<InvalidOperationException>(() =>
            new ActionResolver(new ScriptedActionRandom(7, 1))
                .Resolve(new ActionContext(5), new ActionConfig()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PlayerAttributes(new Dictionary<PlayerAttribute, int>
                { [PlayerAttribute.Catching] = 11 }));
    }

    [Fact]
    public void SeededRunsAreReproducibleAndUseTriangularDiceDifferences()
    {
        var first = new ActionResolver(new SeededActionRandom(17));
        var second = new ActionResolver(new SeededActionRandom(17));
        var counts = new Dictionary<int, int>();
        for (int i = 0; i < 10000; i++)
        {
            ActionResult a = first.Resolve(new ActionContext(5, 5), new ActionConfig());
            ActionResult b = second.Resolve(new ActionContext(5, 5), new ActionConfig());
            Assert.Equal(a.Outcome, b.Outcome);
            Assert.Equal(a.Score, b.Score);
            Assert.Equal(a.PrimaryRoll, b.PrimaryRoll);
            Assert.Equal(a.OpposingRoll, b.OpposingRoll);
            int difference = a.PrimaryRoll - a.OpposingRoll;
            counts[difference] = counts.GetValueOrDefault(difference) + 1;
        }
        Assert.True(counts[0] > counts[5]);
        Assert.True(counts[0] > counts[-5]);
    }

    [Fact]
    public void MechanicProfilesProduceDifferentDistributions()
    {
        var resolver = new ActionResolver(new SeededActionRandom(120));
        int blockNeutral = 0, criticalFailure = 0, criticalSuccess = 0, routineCatch = 0;
        const int trials = 20000;
        for (int i = 0; i < trials; i++)
        {
            ActionResult block = resolver.Resolve(new ActionContext(5, 5), ActionProfiles.Blocking);
            if (block.Outcome == ActionOutcome.Neutral) blockNeutral++;
            if (block.Outcome == ActionOutcome.CriticalFailure) criticalFailure++;
            if (block.Outcome == ActionOutcome.CriticalSuccess) criticalSuccess++;
            ActionResult catchResult = resolver.Resolve(new ActionContext(7), ActionProfiles.Catching);
            if (catchResult.Outcome >= ActionOutcome.Neutral) routineCatch++;
        }
        Assert.True(blockNeutral > trials / 2);
        Assert.InRange(criticalFailure, 950, 1450);
        Assert.InRange(criticalSuccess, 450, 850);
        Assert.True(criticalFailure > criticalSuccess);
        Assert.True(routineCatch > trials * 0.95f);
    }

    [Fact]
    public void StrongerPlayersWinMoreOftenButUnderdogsCanStillWin()
    {
        var resolver = new ActionResolver(new SeededActionRandom(75));
        int strongSuccess = 0, weakSuccess = 0;
        for (int i = 0; i < 10000; i++)
        {
            if (resolver.Resolve(new ActionContext(7, 4), ActionProfiles.Blocking).Outcome > 0)
                strongSuccess++;
            if (resolver.Resolve(new ActionContext(4, 7), ActionProfiles.Blocking).Outcome > 0)
                weakSuccess++;
        }
        Assert.True(strongSuccess > weakSuccess);
        Assert.True(weakSuccess > 0);
    }
}
