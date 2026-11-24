using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class FieldPopulationTests
{
    [Fact]
    public void StressScenePlacesTwentyTwoPlayersOnFieldAndSeventyFiveOnSidelines()
    {
        PlayerSpawn[] field = FieldPopulation.CreateField();
        PlayerSpawn[] sideline = FieldPopulation.CreateSidelines();

        Assert.Equal(22, field.Length);
        Assert.Equal(75, sideline.Length);
        Assert.Equal(97, field.Length + sideline.Length);
        Assert.All(field, spawn =>
        {
            Assert.InRange(spawn.Position.X, 65, 255);
            Assert.InRange(spawn.Position.Y, 75, 105);
        });
        Assert.All(sideline, spawn =>
        {
            Assert.True(spawn.Position.X < 48 || spawn.Position.X > 272);
            Assert.InRange(spawn.Position.Y, 0, 180);
        });
        Assert.Equal(field.Length, field.Select(spawn => spawn.Position).Distinct().Count());
        Assert.Equal(sideline.Length, sideline.Select(spawn => spawn.Position).Distinct().Count());
    }
}
