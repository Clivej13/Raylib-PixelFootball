namespace RaylibPixelFootball.Game;

// The tackle result chooses the carrier's visual response; animations do not
// change the graded resolution or decide whether possession continues.
internal static class BreakTackleAnimations
{
    public static PlayerAnimationKind? ForContact(TackleEffect effect) => effect switch
    {
        TackleEffect.Miss => PlayerAnimationKind.Juke,
        TackleEffect.Broken => PlayerAnimationKind.Truck,
        TackleEffect.Glance => PlayerAnimationKind.StiffArm,
        _ => null
    };

    public static PlayerAnimationKind ForEscapedWrap() => PlayerAnimationKind.Spin;
}
