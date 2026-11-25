namespace RaylibPixelFootball.Game;

internal sealed class RecoveryWeights
{
    public required RecoveryTimingFile Tackled { get; init; }
    public required RecoveryTimingFile MadeTackle { get; init; }
    public required RecoveryTimingFile MissedTackle { get; init; }
    public required RecoveryTimingFile LostBlock { get; init; }
    public required RecoveryTimingFile LostPassRush { get; init; }

    public RecoveryTimingFile For(DownEvent cause) => cause switch
    {
        DownEvent.Tackled => Tackled,
        DownEvent.MadeTackle => MadeTackle,
        DownEvent.MissedTackle => MissedTackle,
        DownEvent.LostBlock => LostBlock,
        DownEvent.LostPassRush => LostPassRush,
        _ => throw new ArgumentOutOfRangeException(nameof(cause))
    };

    public void Validate()
    {
        foreach (DownEvent cause in Enum.GetValues<DownEvent>())
            For(cause).Validate(cause.ToString());
    }
}

internal sealed class RecoveryTimingFile
{
    public required float MinimumDownSeconds { get; init; }
    public required float GetUpPlaybackRate { get; init; }

    public void Validate(string cause)
    {
        if (!float.IsFinite(MinimumDownSeconds) || MinimumDownSeconds < 0f
            || !float.IsFinite(GetUpPlaybackRate) || GetUpPlaybackRate <= 0f)
            throw new InvalidDataException($"Invalid recovery weights for {cause}.");
    }
}

internal sealed class BallBehaviorWeights
{
    public required int MaximumCatchAttempts { get; init; }
    public required float Gravity { get; init; }
    public required float DeflectedVelocityX { get; init; }
    public required float DeflectedVelocityY { get; init; }
    public required float DroppedVelocityY { get; init; }
    public required float CatchRetryCooldownSeconds { get; init; }
    public required float BobbleWindowSeconds { get; init; }
    public required float CarriedOffsetX { get; init; }
    public required float CarriedOffsetY { get; init; }

    public void Validate()
    {
        if (MaximumCatchAttempts < 1 || !float.IsFinite(Gravity) || Gravity < 0f
            || !float.IsFinite(DeflectedVelocityX) || !float.IsFinite(DeflectedVelocityY)
            || !float.IsFinite(DroppedVelocityY)
            || !float.IsFinite(CatchRetryCooldownSeconds) || CatchRetryCooldownSeconds < 0f
            || !float.IsFinite(BobbleWindowSeconds) || BobbleWindowSeconds < 0f
            || !float.IsFinite(CarriedOffsetX) || !float.IsFinite(CarriedOffsetY))
            throw new InvalidDataException("Invalid BallBehavior weights.");
    }
}

internal sealed class EngagementTimingWeights
{
    public required float WinnerRecoverySeconds { get; init; }
    public required float LoserMinimumRecoverySeconds { get; init; }
    public required float WinnerReengageSeconds { get; init; }
    public required float LoserReengageDelaySeconds { get; init; }
    public required float ReleasedReengageSeconds { get; init; }
    public required float TackleRetrySeconds { get; init; }

    public void Validate()
    {
        foreach (float seconds in new[] { WinnerRecoverySeconds,
            LoserMinimumRecoverySeconds, WinnerReengageSeconds,
            LoserReengageDelaySeconds, ReleasedReengageSeconds, TackleRetrySeconds })
            if (!float.IsFinite(seconds) || seconds < 0f)
                throw new InvalidDataException("Invalid EngagementTiming weights.");
    }
}

internal sealed class KickoffWeights
{
    public required float WindupSeconds { get; init; }
    public required float FlightSeconds { get; init; }
    public required float CatchRadius { get; init; }
    public required float ArcHeight { get; init; }
    public required float SpiralFrameSeconds { get; init; }
    public required float LooseBallTimeoutSeconds { get; init; }
    public required float[] BounceFrameSeconds { get; init; }

    public void Validate()
    {
        if (!float.IsFinite(WindupSeconds) || WindupSeconds <= 0f
            || !float.IsFinite(FlightSeconds) || FlightSeconds <= 0f
            || !float.IsFinite(CatchRadius) || CatchRadius <= 0f
            || !float.IsFinite(ArcHeight) || ArcHeight < 0f
            || !float.IsFinite(SpiralFrameSeconds) || SpiralFrameSeconds <= 0f
            || !float.IsFinite(LooseBallTimeoutSeconds) || LooseBallTimeoutSeconds <= 0f
            || BounceFrameSeconds.Length != 8
            || BounceFrameSeconds.Any(value => !float.IsFinite(value) || value <= 0f))
            throw new InvalidDataException("Invalid Kickoff weights.");
    }
}

internal sealed class PlayerRatingWeights
{
    public required int DefaultRating { get; init; }
    public required RatingPattern CoverageBlockShedding { get; init; }
    public required RatingPattern CoverageStrength { get; init; }
    public required RatingPattern CoverageStripBall { get; init; }
    public required RatingPattern ReturnBlocking { get; init; }
    public required RatingPattern ReturnStrength { get; init; }
    public required RatingPattern ReturnCatching { get; init; }
    public required RatingPattern ReturnBallSecurity { get; init; }

    public void Validate()
    {
        if (DefaultRating is < 1 or > 10)
            throw new InvalidDataException("Invalid default player rating.");
        foreach (RatingPattern pattern in new[] { CoverageBlockShedding,
            CoverageStrength, CoverageStripBall, ReturnBlocking, ReturnStrength,
            ReturnCatching, ReturnBallSecurity })
            pattern.Validate();
    }
}

internal sealed class RatingPattern
{
    public required int Base { get; init; }
    public required int Step { get; init; }
    public required int Modulo { get; init; }

    public int At(int index) => Base + index * Step % Modulo;

    public void Validate()
    {
        if (Modulo <= 0 || Step < 0
            || Enumerable.Range(0, 11).Any(index => At(index) is < 1 or > 10))
            throw new InvalidDataException("Invalid kickoff player rating pattern.");
    }
}
