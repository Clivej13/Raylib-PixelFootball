namespace RaylibPixelFootball.Game;

// Sprite sheets are numbered from one; playback indexes from zero.
internal sealed class TackleContactCue
{
    public const int ContactFrameIndex = 3;
    private bool _armed;

    public void Arm() => _armed = true;
    public void Reset() => _armed = false;

    public bool Consume(PlayerAnimationKind carrierAnimation, int frameIndex)
    {
        if (!_armed || (carrierAnimation == PlayerAnimationKind.Tackled
            && frameIndex < ContactFrameIndex))
            return false;
        _armed = false;
        return true;
    }
}
