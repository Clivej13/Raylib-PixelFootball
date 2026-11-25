namespace RaylibPixelFootball.Game;

internal enum DownEvent
{
    Tackled,
    MadeTackle,
    MissedTackle,
    LostBlock,
    LostPassRush
}

internal readonly record struct RecoveryPlan(
    PlayerAnimationKind GetUpAnimation, float MinimumDownSeconds, float GetUpPlaybackRate);

internal static class RecoveryAnimations
{
    public static RecoveryPlan For(DownEvent cause)
    {
        PlayerAnimationKind animation = cause switch
        {
            DownEvent.Tackled => PlayerAnimationKind.GetUpHurt,
            DownEvent.MadeTackle or DownEvent.MissedTackle => PlayerAnimationKind.GetUpFast,
            DownEvent.LostBlock or DownEvent.LostPassRush => PlayerAnimationKind.GetUpSlow,
            _ => throw new ArgumentOutOfRangeException(nameof(cause))
        };
        RecoveryTimingFile timing = ActionProfiles.Weights.Recovery.For(cause);
        return new RecoveryPlan(animation, timing.MinimumDownSeconds,
            timing.GetUpPlaybackRate);
    }
}
