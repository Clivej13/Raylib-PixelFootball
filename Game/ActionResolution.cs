namespace RaylibPixelFootball.Game;

internal enum ActionOutcome
{
    CriticalFailure = -3,
    MajorFailure = -2,
    MinorFailure = -1,
    Neutral = 0,
    MinorSuccess = 1,
    MajorSuccess = 2,
    CriticalSuccess = 3
}

internal readonly record struct ActionModifier(string Name, int Value);
internal readonly record struct ActionConsequence(
    float DisplacementYards = 0f, float RecoverySeconds = 0f,
    float MomentumRetention = 1f);

internal sealed record ActionThresholds(
    int CriticalFailureMax = -6, int MajorFailureMax = -4,
    int MinorFailureMax = -2, int NeutralMax = 1,
    int MinorSuccessMax = 3, int MajorSuccessMax = 5)
{
    public void Validate()
    {
        if (!(CriticalFailureMax < MajorFailureMax
            && MajorFailureMax < MinorFailureMax
            && MinorFailureMax < NeutralMax
            && NeutralMax < MinorSuccessMax
            && MinorSuccessMax < MajorSuccessMax))
            throw new ArgumentException("Action thresholds must increase strictly.");
    }

    public ActionOutcome Classify(float score) => score switch
    {
        _ when score <= CriticalFailureMax => ActionOutcome.CriticalFailure,
        _ when score <= MajorFailureMax => ActionOutcome.MajorFailure,
        _ when score <= MinorFailureMax => ActionOutcome.MinorFailure,
        _ when score <= NeutralMax => ActionOutcome.Neutral,
        _ when score <= MinorSuccessMax => ActionOutcome.MinorSuccess,
        _ when score <= MajorSuccessMax => ActionOutcome.MajorSuccess,
        _ => ActionOutcome.CriticalSuccess
    };
}

internal sealed class ActionConfig
{
    public int BaseDifficulty { get; }
    public int DiceMin { get; }
    public int DiceMax { get; }
    public float PrimaryWeight { get; }
    public float OpposingWeight { get; }
    public float ResolutionInterval { get; }
    public float MaximumDuration { get; }
    public ActionThresholds Thresholds { get; }
    public IReadOnlyDictionary<ActionOutcome, ActionConsequence> Consequences { get; }

    public ActionConfig(int baseDifficulty = 5, int diceMin = 1, int diceMax = 6,
        float primaryWeight = 1f, float opposingWeight = 1f,
        float resolutionInterval = 0f, float maximumDuration = 0f,
        ActionThresholds? thresholds = null,
        IReadOnlyDictionary<ActionOutcome, ActionConsequence>? consequences = null)
    {
        if (baseDifficulty is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(baseDifficulty));
        if (diceMin < 1 || diceMax < diceMin) throw new ArgumentOutOfRangeException(nameof(diceMax));
        if (!float.IsFinite(primaryWeight) || primaryWeight <= 0f)
            throw new ArgumentOutOfRangeException(nameof(primaryWeight));
        if (!float.IsFinite(opposingWeight) || opposingWeight <= 0f)
            throw new ArgumentOutOfRangeException(nameof(opposingWeight));
        if (!float.IsFinite(resolutionInterval) || resolutionInterval < 0f)
            throw new ArgumentOutOfRangeException(nameof(resolutionInterval));
        if (!float.IsFinite(maximumDuration) || maximumDuration < 0f)
            throw new ArgumentOutOfRangeException(nameof(maximumDuration));
        (thresholds ?? new ActionThresholds()).Validate();
        BaseDifficulty = baseDifficulty;
        DiceMin = diceMin;
        DiceMax = diceMax;
        PrimaryWeight = primaryWeight;
        OpposingWeight = opposingWeight;
        ResolutionInterval = resolutionInterval;
        MaximumDuration = maximumDuration;
        Thresholds = thresholds ?? new ActionThresholds();
        Consequences = consequences ?? new Dictionary<ActionOutcome, ActionConsequence>();
    }

    public ActionConsequence ConsequenceFor(ActionOutcome outcome) =>
        Consequences.TryGetValue(outcome, out ActionConsequence consequence)
            ? consequence : new ActionConsequence();
}

internal sealed record ActionContext(
    int PrimarySkill, int? OpposingSkill = null,
    IReadOnlyList<ActionModifier>? Modifiers = null);

internal sealed record ActionResult(
    ActionOutcome Outcome, float Score, int PrimarySkill, int OpposingSkill,
    int PrimaryRoll, int OpposingRoll, IReadOnlyList<ActionModifier> AppliedModifiers)
{
    public int ModifierTotal => AppliedModifiers.Sum(modifier => modifier.Value);
}

internal interface IActionRandom
{
    int NextInclusive(int minimum, int maximum);
}

internal sealed class SeededActionRandom : IActionRandom
{
    private readonly Random _random;
    public SeededActionRandom(int seed) => _random = new Random(seed);
    public int NextInclusive(int minimum, int maximum) => _random.Next(minimum, maximum + 1);
}

internal sealed class SharedActionRandom : IActionRandom
{
    public int NextInclusive(int minimum, int maximum) => Random.Shared.Next(minimum, maximum + 1);
}

// Performs arithmetic only. Gameplay systems interpret the returned result.
internal sealed class ActionResolver
{
    private readonly IActionRandom _random;
    public ActionResolver(IActionRandom? random = null) => _random = random ?? new SharedActionRandom();

    public ActionResult Resolve(ActionContext context, ActionConfig config)
    {
        if (context.PrimarySkill is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(context.PrimarySkill));
        int opposing = context.OpposingSkill ?? config.BaseDifficulty;
        if (opposing is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(context.OpposingSkill));
        ActionModifier[] modifiers = context.Modifiers?.ToArray() ?? [];
        int first = _random.NextInclusive(config.DiceMin, config.DiceMax);
        int second = _random.NextInclusive(config.DiceMin, config.DiceMax);
        if (first < config.DiceMin || first > config.DiceMax
            || second < config.DiceMin || second > config.DiceMax)
            throw new InvalidOperationException("Action RNG returned an out-of-range value.");
        float score = context.PrimarySkill * config.PrimaryWeight
            - opposing * config.OpposingWeight + first - second
            + modifiers.Sum(modifier => modifier.Value);
        return new ActionResult(config.Thresholds.Classify(score), score,
            context.PrimarySkill, opposing, first, second, modifiers);
    }
}
