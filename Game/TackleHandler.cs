namespace RaylibPixelFootball.Game;

internal enum TackleEffect
{
    Miss, Broken, Glance, Wrap, Controlled, Complete, Dominant
}

internal sealed record TackleResult(ActionResult Action, TackleEffect Effect);

internal sealed class TackleHandler
{
    private readonly ActionResolver _resolver;
    private readonly ActionConfig _profile;

    public TackleHandler(ActionResolver resolver, ActionConfig? profile = null)
    {
        _resolver = resolver;
        _profile = profile ?? ActionProfiles.Tackling;
    }

    public TackleResult Resolve(PlayerAttributes tackler, PlayerAttributes carrier,
        IReadOnlyList<ActionModifier>? modifiers = null)
    {
        ActionResult result = _resolver.Resolve(new ActionContext(
            tackler.Get(PlayerAttribute.Tackling), carrier.Get(PlayerAttribute.BreakTackle),
            modifiers), _profile);
        TackleEffect effect = result.Outcome switch
        {
            ActionOutcome.CriticalFailure => TackleEffect.Miss,
            ActionOutcome.MajorFailure => TackleEffect.Broken,
            ActionOutcome.MinorFailure => TackleEffect.Glance,
            ActionOutcome.Neutral => TackleEffect.Wrap,
            ActionOutcome.MinorSuccess => TackleEffect.Controlled,
            ActionOutcome.MajorSuccess => TackleEffect.Complete,
            _ => TackleEffect.Dominant
        };
        return new TackleResult(result, effect);
    }
}

internal enum TackleStruggleOutcome { Active, Tackled, Escaped }

internal sealed class TackleStruggle
{
    private readonly TackleHandler _handler;
    private readonly PlayerAttributes _tackler;
    private readonly PlayerAttributes _carrier;
    private readonly ActionConfig _profile;
    private readonly int _initialControl;
    private float _timer;

    public TackleStruggleOutcome Outcome { get; private set; } = TackleStruggleOutcome.Active;
    public TackleResult? LastResult { get; private set; }
    public float Elapsed { get; private set; }

    public TackleStruggle(TackleHandler handler, PlayerAttributes tackler,
        PlayerAttributes carrier, int initialControl = 0, ActionConfig? profile = null)
    {
        _handler = handler;
        _tackler = tackler;
        _carrier = carrier;
        _initialControl = initialControl;
        _profile = profile ?? ActionProfiles.Tackling;
    }

    public TackleStruggleOutcome Update(float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        if (Outcome != TackleStruggleOutcome.Active) return Outcome;
        float remaining = Math.Min(deltaTime, _profile.MaximumDuration - Elapsed);
        while (remaining > 0f && Outcome == TackleStruggleOutcome.Active)
        {
            float step = Math.Min(remaining, _profile.ResolutionInterval - _timer);
            _timer += step;
            Elapsed += step;
            remaining -= step;
            if (_timer + 0.00001f < _profile.ResolutionInterval) continue;
            _timer = 0f;
            LastResult = _handler.Resolve(_tackler, _carrier,
                [new ActionModifier("Wrap control",
                    _initialControl * ActionProfiles.Weights.Situational.WrapControl)]);
            Outcome = LastResult.Effect switch
            {
                TackleEffect.Complete or TackleEffect.Dominant => TackleStruggleOutcome.Tackled,
                TackleEffect.Miss or TackleEffect.Broken => TackleStruggleOutcome.Escaped,
                _ => TackleStruggleOutcome.Active
            };
        }
        if (Outcome == TackleStruggleOutcome.Active
            && Elapsed + 0.00001f >= _profile.MaximumDuration)
            Outcome = LastResult?.Action.Score >= 0f
                ? TackleStruggleOutcome.Tackled : TackleStruggleOutcome.Escaped;
        return Outcome;
    }
}

internal sealed class BallSecurityHandler
{
    private readonly ActionResolver _resolver;
    private readonly ActionConfig _profile;

    public BallSecurityHandler(ActionResolver resolver, ActionConfig? profile = null)
    {
        _resolver = resolver;
        _profile = profile ?? ActionProfiles.BallSecurity;
    }

    public ActionResult Resolve(PlayerAttributes carrier, PlayerAttributes defender)
        => _resolver.Resolve(new ActionContext(
            carrier.Get(PlayerAttribute.BallSecurity), defender.Get(PlayerAttribute.StripBall),
            [new ActionModifier("Dominant tackle",
                ActionProfiles.Weights.Situational.DominantTackle)]), _profile);

    public static bool IsFumble(ActionResult result) =>
        result.Outcome == ActionOutcome.CriticalFailure;
}
