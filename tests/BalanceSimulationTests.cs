using System.Numerics;
using RaylibPixelFootball.Game;
using Xunit;
using Xunit.Abstractions;

namespace RaylibPixelFootball.Tests;

// Seeded headless sampling keeps the first balance pass measurable and repeatable.
public sealed class BalanceSimulationTests
{
    private readonly ITestOutputHelper _output;

    public BalanceSimulationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void SampleKickoffBlockingAndCatching()
    {
        const int samples = 5000;
        var resolver = new ActionResolver(new SeededActionRandom(20261124));
        int sheds = 0, pancakes = 0, completed = 0, criticalDrops = 0;
        float blockSeconds = 0f, displacement = 0f, recovery = 0f;
        Vector2 defender = new(0f, 8f), blocker = Vector2.Zero, carrier = new(0f, -20f);

        for (int i = 0; i < samples; i++)
        {
            var contest = new BlockContest(5, 5, 5, 5, resolver);
            while (contest.Outcome == BlockOutcome.Active)
                contest.Update(0.05f, defender, blocker, carrier);
            blockSeconds += contest.Elapsed;
            displacement += contest.TotalDisplacementYards;
            if (contest.Outcome == BlockOutcome.CoverageWins) sheds++;
            if (contest.Outcome == BlockOutcome.BlockerWins) pancakes++;
        }

        var catchHandler = new CatchHandler(resolver);
        var receiver = new PlayerAttributes(new Dictionary<PlayerAttribute, int>
            { [PlayerAttribute.Catching] = 7 });
        for (int i = 0; i < samples; i++)
        {
            var ball = new BallPossession();
            ball.BeginFlight(Vector2.Zero, BallPlayType.Kickoff);
            ActionResult result = catchHandler.Attempt(ball, receiver, default, 0, Vector2.Zero);
            if (ball.IsCarried)
            {
                completed++;
                recovery += ball.RecoveryRemaining;
            }
            if (result.Outcome == ActionOutcome.CriticalFailure) criticalDrops++;
        }

        float meanDuration = blockSeconds / samples;
        float shedRate = sheds / (float)samples;
        float pancakeRate = pancakes / (float)samples;
        float meanDisplacement = displacement / samples;
        float completionRate = completed / (float)samples;
        float criticalDropRate = criticalDrops / (float)samples;
        float meanRecovery = recovery / Math.Max(1, completed);
        _output.WriteLine(
            $"matched blocks: duration={meanDuration:0.000}s shed={shedRate:P2} pancake={pancakeRate:P2} displacement={meanDisplacement:+0.000;-0.000;0.000} yd");
        _output.WriteLine(
            $"routine catches: completion={completionRate:P2} critical drop={criticalDropRate:P2} recovery={meanRecovery:0.000}s");

        Assert.InRange(meanDuration, 1.0f, 1.25f);
        Assert.InRange(shedRate, 0.85f, 0.98f);
        Assert.InRange(pancakeRate, 0.02f, 0.12f);
        Assert.True(shedRate > pancakeRate);
        Assert.InRange(completionRate, 0.95f, 1f);
        Assert.InRange(criticalDropRate, 0f, 0.01f);
    }
}
