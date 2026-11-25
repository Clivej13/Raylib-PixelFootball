using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class BaselineTackleOddsTests
{
    [Fact]
    public void KickoffPlayersStartWithEqualTacklingRatings()
    {
        for (int index = 0; index < 11; index++)
            Assert.Equal(5, PlayerAttributes.KickoffCoverage(index)
                .Get(PlayerAttribute.Tackling));
        for (int index = 0; index < 11; index++)
            Assert.Equal(5, PlayerAttributes.KickoffReturnTeam(index)
                .Get(PlayerAttribute.BreakTackle));
    }

    [Fact]
    public void EqualRatingsProduceFifteenImmediateTacklesAndFifteenBadContactsPerSixtyFour()
    {
        var attributes = new PlayerAttributes();
        int complete = 0, bad = 0, struggle = 0;
        for (int first = 1; first <= 8; first++)
            for (int second = 1; second <= 8; second++)
            {
                var handler = new TackleHandler(new ActionResolver(
                    new ScriptedActionRandom(first, second)));
                TackleEffect effect = handler.Resolve(attributes, attributes).Effect;
                if (effect is TackleEffect.Complete or TackleEffect.Dominant) complete++;
                else if (effect is TackleEffect.Miss or TackleEffect.Broken
                    or TackleEffect.Glance) bad++;
                else struggle++;
            }
        Assert.Equal(15, complete);
        Assert.Equal(15, bad);
        Assert.Equal(34, struggle);
    }

    [Fact]
    public void BaselineContactIncludingWrapStrugglesFavorsTheTackler()
    {
        const int attempts = 10000;
        var attributes = new PlayerAttributes();
        var handler = new TackleHandler(new ActionResolver(new SeededActionRandom(5412)));
        int tackled = 0;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            TackleEffect first = handler.Resolve(attributes, attributes).Effect;
            if (first is TackleEffect.Complete or TackleEffect.Dominant)
                tackled++;
            else if (first is TackleEffect.Wrap or TackleEffect.Controlled)
            {
                var struggle = new TackleStruggle(handler, attributes, attributes,
                    first == TackleEffect.Controlled ? 1 : 0);
                if (struggle.Update(ActionProfiles.Tackling.MaximumDuration)
                    == TackleStruggleOutcome.Tackled)
                    tackled++;
            }
        }
        Assert.InRange(tackled, 5900, 6300);
    }
}
