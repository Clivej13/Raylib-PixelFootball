namespace RaylibPixelFootball.TwoD;

// Animation timing belongs to each entity, never to a shared texture.
public sealed class SpritePlayback
{
    private readonly SpriteSheetLayout _layout;
    private readonly float _framesPerSecond;
    private float _fractionalFrames;
    private bool _loop;
    private int _frameCount;

    public int AnimationIndex { get; private set; }
    public int FrameIndex { get; private set; }
    public bool IsComplete { get; private set; }

    public SpritePlayback(SpriteSheetLayout layout, float framesPerSecond)
    {
        _layout = layout;
        if (!float.IsFinite(framesPerSecond) || framesPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        _framesPerSecond = framesPerSecond;
        _frameCount = layout.FramesPerAnimation;
    }

    public void Play(int animationIndex, bool loop, bool restart = false, bool preservePhase = false,
        int? frameCount = null)
    {
        _layout.SourceRectangle(animationIndex, 0);
        int nextFrameCount = frameCount ?? _layout.FramesPerAnimation;
        if (nextFrameCount <= 0 || nextFrameCount > _layout.FramesPerAnimation)
            throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (!restart && _loop == loop && _frameCount == nextFrameCount && !IsComplete)
        {
            if (AnimationIndex == animationIndex) return;
            if (preservePhase)
            {
                AnimationIndex = animationIndex;
                return;
            }
        }
        AnimationIndex = animationIndex;
        _frameCount = nextFrameCount;
        FrameIndex = 0;
        _fractionalFrames = 0;
        IsComplete = false;
        _loop = loop;
    }

    public void StopAtFirstFrame()
    {
        FrameIndex = 0;
        _fractionalFrames = 0;
    }

    public void Update(float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        if (IsComplete) return;
        _fractionalFrames += deltaTime * _framesPerSecond;
        int frames = (int)(_fractionalFrames + 0.00001f);
        _fractionalFrames = Math.Max(0f, _fractionalFrames - frames);
        if (frames == 0) return;
        if (_loop)
            FrameIndex = (FrameIndex + frames) % _frameCount;
        else if (FrameIndex + frames >= _frameCount)
        {
            FrameIndex = _frameCount - 1;
            IsComplete = true;
        }
        else
            FrameIndex += frames;
    }
}
