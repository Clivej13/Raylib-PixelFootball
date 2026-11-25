using System.Text.Json;
using System.Text.Json.Nodes;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class ActionWeightsJsonTests
{
    private static string JsonPath => Path.Combine(AppContext.BaseDirectory,
        "action_weights.json");

    [Fact]
    public void ShippedFileLoadsEveryActionProfileAndGameplayWeightGroup()
    {
        ActionWeights weights = ActionWeights.Load(JsonPath);
        Assert.Equal(10, weights.Blocking.DiceMax);
        Assert.Equal(4, weights.Catching.BaseDifficulty);
        Assert.Equal(8, weights.Tackling.DiceMax);
        Assert.Equal(-8, weights.BallSecurity.Thresholds.CriticalFailureMax);
        Assert.Equal(1, weights.Situational.UnsecuredBall);
        Assert.Equal(0.08f, weights.BlockMotion.StrengthDisplacementPerPoint);
        Assert.Equal(0.35f, weights.TackleEffects.WrapMomentumRetention);
        Assert.Equal(3, weights.BallBehavior.MaximumCatchAttempts);
        Assert.Equal(5, weights.PlayerRatings.DefaultRating);
        Assert.Equal(0.5f, weights.EngagementTiming.TackleRetrySeconds);
        Assert.Equal(2.2f, weights.Kickoff.FlightSeconds);
        Assert.Equal(0.75f, weights.Recovery.Tackled.GetUpPlaybackRate);
    }

    [Fact]
    public void ChangingJsonWeightChangesTheResolvedScore()
    {
        JsonNode root = JsonNode.Parse(File.ReadAllText(JsonPath))!;
        root["Tackling"]!["PrimaryWeight"] = 2.0;
        ActionWeights edited = ActionWeights.Parse(root.ToJsonString());
        var resolver = new ActionResolver(new ScriptedActionRandom(4, 4));
        ActionResult result = resolver.Resolve(new ActionContext(5, 5), edited.Tackling);
        Assert.Equal(5f, result.Score);
        Assert.Equal(ActionOutcome.CriticalSuccess, result.Outcome);
    }

    [Fact]
    public void MissingOrInvalidWeightsFailAtLoad()
    {
        JsonNode missing = JsonNode.Parse(File.ReadAllText(JsonPath))!;
        missing.AsObject().Remove("Catching");
        Assert.Throws<JsonException>(() => ActionWeights.Parse(missing.ToJsonString()));

        JsonNode invalid = JsonNode.Parse(File.ReadAllText(JsonPath))!;
        invalid["Tackling"]!["Thresholds"]!["MajorFailureMax"] = -100;
        Assert.Throws<ArgumentException>(() => ActionWeights.Parse(invalid.ToJsonString()));
    }
}
