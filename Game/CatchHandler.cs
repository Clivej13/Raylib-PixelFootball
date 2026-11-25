using System.Numerics;

namespace RaylibPixelFootball.Game;

// Positive values help the receiver; negative values make the catch harder.
internal readonly record struct CatchFactors(
    int PassAccuracy = 0, int BallVelocity = 0, int DefensiveCoverage = 0,
    int DefensiveContact = 0, int ReceiverMovement = 0, int BodyPosition = 0,
    int CatchRadius = 0, int Fatigue = 0)
{
    public IReadOnlyList<ActionModifier> ToModifiers() =>
    [
        new("Pass accuracy", PassAccuracy),
        new("Ball velocity", BallVelocity),
        new("Defensive coverage", DefensiveCoverage),
        new("Defensive contact", DefensiveContact),
        new("Receiver movement", ReceiverMovement),
        new("Body position", BodyPosition),
        new("Catch radius", CatchRadius),
        new("Fatigue", Fatigue)
    ];
}

internal sealed class CatchHandler
{
    private readonly ActionResolver _resolver;
    private readonly ActionConfig _profile;

    public CatchHandler(ActionResolver resolver, ActionConfig? profile = null)
    {
        _resolver = resolver;
        _profile = profile ?? ActionProfiles.Catching;
    }

    public ActionResult Attempt(BallPossession ball, PlayerAttributes attributes,
        CatchFactors factors, int playerId, Vector2 position)
    {
        ActionResult result = _resolver.Resolve(new ActionContext(
            attributes.Get(PlayerAttribute.Catching), Modifiers: factors.ToModifiers()), _profile);
        ball.ApplyCatch(result, _profile.ConsequenceFor(result.Outcome), playerId, position);
        return result;
    }
}
