using System.Numerics;

namespace RaylibPixelFootball.Game;

internal enum BlockOutcome { Active, BlockerWins, CoverageWins, Released }

// One continuous engagement; the shared resolver decides grades, this class applies block rules.
internal sealed class BlockContest
{
    public static float ContactRadius => PlayerCollision.MinimumSeparation
        + ActionProfiles.Weights.BlockMotion.ContactMargin;
    public static float ReleaseRadius => ActionProfiles.Weights.BlockMotion.ReleaseRadius;
    public static float TackleRadius => PlayerCollision.MinimumSeparation
        + ActionProfiles.Weights.BlockMotion.TackleMargin;
    private readonly int _blocking;
    private readonly int _blockShedding;
    private readonly int _blockerStrength;
    private readonly int _defenderStrength;
    private readonly ActionResolver _resolver;
    private readonly ActionConfig _profile;
    private float _timer;
    private float _holdDuration;
    private float _pendingSeconds;
    private Vector2 _pendingMove;

    public BlockOutcome Outcome { get; private set; } = BlockOutcome.Active;
    public ActionResult? LastResult { get; private set; }
    public float Elapsed { get; private set; }
    public float HoldDuration => _holdDuration;
    public Vector2 MovementDelta { get; private set; }
    public float TotalDisplacementYards { get; private set; }

    public BlockContest(int blocking, int blockShedding, int blockerStrength, int defenderStrength,
        ActionResolver resolver, ActionConfig? profile = null)
    {
        foreach (int rating in new[] { blocking, blockShedding, blockerStrength, defenderStrength })
            if (rating is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(blocking));
        _blocking = blocking;
        _blockShedding = blockShedding;
        _blockerStrength = blockerStrength;
        _defenderStrength = defenderStrength;
        _resolver = resolver;
        _profile = profile ?? ActionProfiles.Blocking;
        if (_profile.ResolutionInterval <= 0f || _profile.MaximumDuration <= 0f)
            throw new ArgumentException("Blocking needs an interval and maximum duration.", nameof(profile));
        BlockMotionWeights motion = ActionProfiles.Weights.BlockMotion;
        _holdDuration = Math.Clamp(_profile.MaximumDuration
            + (blocking - blockShedding) * motion.HoldSkillSeconds,
            motion.MinimumHoldSeconds, motion.MaximumHoldSeconds);
    }

    public BlockOutcome Update(float deltaTime, Vector2 coverage, Vector2 blocker, Vector2 carrier)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        MovementDelta = Vector2.Zero;
        if (Outcome != BlockOutcome.Active) return Outcome;
        if (ShouldRelease(coverage, blocker, carrier))
            return Outcome = BlockOutcome.Released;

        float remaining = deltaTime;
        while (remaining > 0f && Outcome == BlockOutcome.Active)
        {
            float step = Math.Min(remaining, Math.Min(
                _profile.ResolutionInterval - _timer, _holdDuration - Elapsed));
            if (step <= 0f) break;
            AdvancePendingMove(step);
            Elapsed += step;
            _timer += step;
            remaining -= step;

            if (_timer + 0.00001f >= _profile.ResolutionInterval)
            {
                _timer = 0f;
                ResolveCheck(coverage + MovementDelta, blocker + MovementDelta, carrier);
            }
            if (Outcome == BlockOutcome.Active && Elapsed + 0.00001f >= _holdDuration)
                Outcome = BlockOutcome.CoverageWins;
        }
        return Outcome;
    }

    private void AdvancePendingMove(float step)
    {
        if (_pendingSeconds <= 0f) return;
        float portion = Math.Min(1f, step / _pendingSeconds);
        Vector2 shift = _pendingMove * portion;
        MovementDelta += shift;
        _pendingMove -= shift;
        _pendingSeconds = Math.Max(0f, _pendingSeconds - step);
    }

    private void ResolveCheck(Vector2 coverage, Vector2 blocker, Vector2 carrier)
    {
        // Contact already places one player closer to the carrier. Only a clear
        // lateral mismatch changes the technical contest; strength affects motion below.
        int lane = MathF.Abs(blocker.X - coverage.X)
            > ActionProfiles.Weights.BlockMotion.LaneLeverageDistance
            ? ActionProfiles.Weights.Situational.LaneLeverage : 0;
        LastResult = _resolver.Resolve(new ActionContext(_blocking, _blockShedding,
            [new ActionModifier("Lane leverage", lane)]), _profile);
        ActionConsequence consequence = _profile.ConsequenceFor(LastResult.Outcome);
        if (consequence.DisplacementYards != 0f)
        {
            BlockMotionWeights motion = ActionProfiles.Weights.BlockMotion;
            float strengthFactor = Math.Clamp(
                1f + (_blockerStrength - _defenderStrength) * motion.StrengthDisplacementPerPoint,
                motion.MinimumStrengthFactor, motion.MaximumStrengthFactor);
            Vector2 direction = coverage - blocker;
            if (direction.LengthSquared() < 0.0001f) direction = Vector2.UnitY;
            else direction = Vector2.Normalize(direction);
            float yards = consequence.DisplacementYards * strengthFactor;
            _pendingMove += direction * (yards * PlayerEntity.WorldUnitsPerYard);
            _pendingSeconds = _profile.ResolutionInterval;
            TotalDisplacementYards += yards;
        }

        if (LastResult.Outcome == ActionOutcome.CriticalFailure)
            Outcome = BlockOutcome.CoverageWins;
        else if (LastResult.Outcome == ActionOutcome.CriticalSuccess)
            Outcome = BlockOutcome.BlockerWins;
        else
        {
            BlockMotionWeights motion = ActionProfiles.Weights.BlockMotion;
            _holdDuration = Math.Clamp(_holdDuration
                + (int)LastResult.Outcome * motion.OutcomeHoldAdjustmentSeconds,
                motion.MinimumHoldSeconds, motion.MaximumHoldSeconds);
        }
    }

    private static bool ShouldRelease(Vector2 coverage, Vector2 blocker, Vector2 carrier) =>
        Vector2.DistanceSquared(coverage, blocker) > ReleaseRadius * ReleaseRadius
        || carrier.Y > (coverage.Y + blocker.Y) / 2f
            + ActionProfiles.Weights.BlockMotion.CarrierReleaseDistance;
}
