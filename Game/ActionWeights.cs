using System.Text.Json;

namespace RaylibPixelFootball.Game;

internal sealed class ActionWeights
{
    public ActionConfig Blocking { get; }
    public ActionConfig Catching { get; }
    public ActionConfig Tackling { get; }
    public ActionConfig BallSecurity { get; }
    public SituationalWeights Situational { get; }
    public BlockMotionWeights BlockMotion { get; }
    public TackleEffectWeights TackleEffects { get; }
    public RecoveryWeights Recovery { get; }
    public BallBehaviorWeights BallBehavior { get; }
    public EngagementTimingWeights EngagementTiming { get; }
    public KickoffWeights Kickoff { get; }
    public PlayerRatingWeights PlayerRatings { get; }

    private ActionWeights(ActionWeightsFile file)
    {
        Blocking = file.Blocking.ToConfig("Blocking", continuous: true);
        Catching = file.Catching.ToConfig("Catching", continuous: false);
        Tackling = file.Tackling.ToConfig("Tackling", continuous: true);
        BallSecurity = file.BallSecurity.ToConfig("BallSecurity", continuous: false);
        Situational = file.Situational;
        BlockMotion = file.BlockMotion;
        TackleEffects = file.TackleEffects;
        Recovery = file.Recovery;
        BallBehavior = file.BallBehavior;
        EngagementTiming = file.EngagementTiming;
        Kickoff = file.Kickoff;
        PlayerRatings = file.PlayerRatings;
        Situational.Validate();
        BlockMotion.Validate();
        TackleEffects.Validate();
        Recovery.Validate();
        BallBehavior.Validate();
        EngagementTiming.Validate();
        Kickoff.Validate();
        PlayerRatings.Validate();
    }

    public static ActionWeights Load(string path) => Parse(File.ReadAllText(path));

    public static ActionWeights Parse(string json)
    {
        var file = JsonSerializer.Deserialize<ActionWeightsFile>(json)
            ?? throw new JsonException("action_weights.json must contain an object.");
        return new ActionWeights(file);
    }
}

internal static class ActionProfiles
{
    private static readonly Lazy<ActionWeights> Loaded = new(() => ActionWeights.Load(
        Path.Combine(AppContext.BaseDirectory, "action_weights.json")));

    public static ActionWeights Weights => Loaded.Value;
    public static ActionConfig Blocking => Weights.Blocking;
    public static ActionConfig Catching => Weights.Catching;
    public static ActionConfig Tackling => Weights.Tackling;
    public static ActionConfig BallSecurity => Weights.BallSecurity;
}

internal sealed class ActionWeightsFile
{
    public required ActionProfileFile Blocking { get; init; }
    public required ActionProfileFile Catching { get; init; }
    public required ActionProfileFile Tackling { get; init; }
    public required ActionProfileFile BallSecurity { get; init; }
    public required SituationalWeights Situational { get; init; }
    public required BlockMotionWeights BlockMotion { get; init; }
    public required TackleEffectWeights TackleEffects { get; init; }
    public required RecoveryWeights Recovery { get; init; }
    public required BallBehaviorWeights BallBehavior { get; init; }
    public required EngagementTimingWeights EngagementTiming { get; init; }
    public required KickoffWeights Kickoff { get; init; }
    public required PlayerRatingWeights PlayerRatings { get; init; }
}

internal sealed class ActionProfileFile
{
    public required int BaseDifficulty { get; init; }
    public required int DiceMin { get; init; }
    public required int DiceMax { get; init; }
    public required float PrimaryWeight { get; init; }
    public required float OpposingWeight { get; init; }
    public required float ResolutionInterval { get; init; }
    public required float MaximumDuration { get; init; }
    public required ActionThresholdFile Thresholds { get; init; }
    public required Dictionary<string, ActionConsequenceFile> Consequences { get; init; }

    public ActionConfig ToConfig(string name, bool continuous)
    {
        if (continuous && (ResolutionInterval <= 0f || MaximumDuration <= 0f))
            throw new InvalidDataException($"{name} needs positive interval and duration.");
        var consequences = new Dictionary<ActionOutcome, ActionConsequence>();
        foreach (var (label, value) in Consequences)
        {
            if (!Enum.TryParse(label, ignoreCase: false, out ActionOutcome outcome)
                || !Enum.IsDefined(outcome))
                throw new InvalidDataException($"{name} has an unknown outcome: {label}.");
            value.Validate(name, label);
            consequences.Add(outcome, new ActionConsequence(
                value.DisplacementYards, value.RecoverySeconds, value.MomentumRetention));
        }
        foreach (ActionOutcome outcome in Enum.GetValues<ActionOutcome>())
            if (!consequences.ContainsKey(outcome))
                throw new InvalidDataException($"{name} is missing {outcome} consequences.");
        return new ActionConfig(BaseDifficulty, DiceMin, DiceMax, PrimaryWeight,
            OpposingWeight, ResolutionInterval, MaximumDuration, Thresholds.ToThresholds(),
            consequences);
    }
}

internal sealed class ActionThresholdFile
{
    public required int CriticalFailureMax { get; init; }
    public required int MajorFailureMax { get; init; }
    public required int MinorFailureMax { get; init; }
    public required int NeutralMax { get; init; }
    public required int MinorSuccessMax { get; init; }
    public required int MajorSuccessMax { get; init; }

    public ActionThresholds ToThresholds() => new(CriticalFailureMax, MajorFailureMax,
        MinorFailureMax, NeutralMax, MinorSuccessMax, MajorSuccessMax);
}

internal sealed class ActionConsequenceFile
{
    public float DisplacementYards { get; init; }
    public float RecoverySeconds { get; init; }
    public float MomentumRetention { get; init; } = 1f;

    public void Validate(string profile, string outcome)
    {
        if (!float.IsFinite(DisplacementYards)
            || !float.IsFinite(RecoverySeconds) || RecoverySeconds < 0f
            || !float.IsFinite(MomentumRetention)
            || MomentumRetention is < 0f or > 1f)
            throw new InvalidDataException(
                $"{profile}/{outcome} has invalid consequence values.");
    }
}

internal sealed class SituationalWeights
{
    public required int LaneLeverage { get; init; }
    public required int UnsecuredBall { get; init; }
    public required int WrapControl { get; init; }
    public required int DominantTackle { get; init; }
    public required int CatchBallVelocity { get; init; }
    public required int CatchDefensiveCoverage { get; init; }
    public required int CatchDefensiveContact { get; init; }
    public required int CatchReceiverMovement { get; init; }
    public required int CatchBodyPosition { get; init; }
    public required int CatchRadius { get; init; }
    public required float CatchCoverageRadius { get; init; }
    public required float CatchContactRadius { get; init; }
    public required float CatchMovementThresholdSquared { get; init; }
    public required float CatchRadiusPenaltyDistanceSquared { get; init; }

    public void Validate()
    {
        foreach (float value in new[] { CatchCoverageRadius, CatchContactRadius,
            CatchMovementThresholdSquared, CatchRadiusPenaltyDistanceSquared })
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidDataException("Invalid catch context thresholds.");
    }
}

internal sealed class BlockMotionWeights
{
    public required float ContactMargin { get; init; }
    public required float ReleaseRadius { get; init; }
    public required float TackleMargin { get; init; }
    public required float LaneLeverageDistance { get; init; }
    public required float CarrierReleaseDistance { get; init; }
    public required float HoldSkillSeconds { get; init; }
    public required float MinimumHoldSeconds { get; init; }
    public required float MaximumHoldSeconds { get; init; }
    public required float StrengthDisplacementPerPoint { get; init; }
    public required float MinimumStrengthFactor { get; init; }
    public required float MaximumStrengthFactor { get; init; }
    public required float OutcomeHoldAdjustmentSeconds { get; init; }

    public void Validate()
    {
        if (!float.IsFinite(ContactMargin) || ContactMargin < 0f
            || !float.IsFinite(ReleaseRadius) || ReleaseRadius <= 0f
            || !float.IsFinite(TackleMargin) || TackleMargin < 0f
            || !float.IsFinite(LaneLeverageDistance) || LaneLeverageDistance < 0f
            || !float.IsFinite(CarrierReleaseDistance) || CarrierReleaseDistance < 0f
            || !float.IsFinite(HoldSkillSeconds)
            || !float.IsFinite(MinimumHoldSeconds) || MinimumHoldSeconds <= 0f
            || !float.IsFinite(MaximumHoldSeconds) || MaximumHoldSeconds < MinimumHoldSeconds
            || !float.IsFinite(StrengthDisplacementPerPoint)
            || !float.IsFinite(MinimumStrengthFactor) || MinimumStrengthFactor <= 0f
            || !float.IsFinite(MaximumStrengthFactor)
            || MaximumStrengthFactor < MinimumStrengthFactor
            || !float.IsFinite(OutcomeHoldAdjustmentSeconds))
            throw new InvalidDataException("Invalid BlockMotion weights.");
    }
}

internal sealed class TackleEffectWeights
{
    public required float MissedTackleDownSeconds { get; init; }
    public required float GlanceRecoverySeconds { get; init; }
    public required float GlanceMomentumRetention { get; init; }
    public required float WrapRecoverySeconds { get; init; }
    public required float WrapMomentumRetention { get; init; }
    public required float EscapeRecoverySeconds { get; init; }
    public required float EscapeMomentumRetention { get; init; }
    public required float CompletedTackleDownSeconds { get; init; }
    public required float DominantTackleDownSeconds { get; init; }

    public void Validate()
    {
        foreach (float seconds in new[] { MissedTackleDownSeconds,
            GlanceRecoverySeconds, WrapRecoverySeconds, EscapeRecoverySeconds,
            CompletedTackleDownSeconds, DominantTackleDownSeconds })
            if (!float.IsFinite(seconds) || seconds < 0f)
                throw new InvalidDataException("Invalid TackleEffects duration.");
        foreach (float retention in new[] { GlanceMomentumRetention,
            WrapMomentumRetention, EscapeMomentumRetention })
            if (!float.IsFinite(retention) || retention is < 0f or > 1f)
                throw new InvalidDataException("Invalid TackleEffects momentum retention.");
    }
}
