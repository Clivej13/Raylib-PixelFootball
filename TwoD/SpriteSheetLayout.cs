using Raylib_cs;

namespace RaylibPixelFootball.TwoD;

// A horizontal strip of directional animations. SourceScale describes imported art;
// rendering always uses the logical frame size.
public sealed class SpriteSheetLayout
{
    public int FrameSize { get; }
    public int SourceScale { get; }
    public int FramesPerAnimation { get; }
    public int AnimationCount { get; }
    public int SourceFrameSize => checked(FrameSize * SourceScale);
    public int ExpectedWidth => checked(SourceFrameSize * FramesPerAnimation * AnimationCount);
    public int ExpectedHeight => SourceFrameSize;

    public SpriteSheetLayout(int frameSize, int sourceScale, int framesPerAnimation, int animationCount)
    {
        if (frameSize <= 0 || sourceScale <= 0 || framesPerAnimation <= 0 || animationCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameSize), "Sprite dimensions and counts must be positive.");
        FrameSize = frameSize;
        SourceScale = sourceScale;
        FramesPerAnimation = framesPerAnimation;
        AnimationCount = animationCount;
    }

    public void ValidateImageSize(int width, int height)
    {
        if (width != ExpectedWidth || height != ExpectedHeight)
            throw new InvalidDataException(
                $"Expected {ExpectedWidth}x{ExpectedHeight} for {AnimationCount} animations " +
                $"of {FramesPerAnimation} frames at {FrameSize}x{FrameSize} logical pixels " +
                $"and {SourceScale}x source scale; got {width}x{height}.");
    }

    public Rectangle SourceRectangle(int animation, int frame)
    {
        if ((uint)animation >= (uint)AnimationCount)
            throw new ArgumentOutOfRangeException(nameof(animation));
        if ((uint)frame >= (uint)FramesPerAnimation)
            throw new ArgumentOutOfRangeException(nameof(frame));
        return new Rectangle(
            checked((animation * FramesPerAnimation + frame) * SourceFrameSize),
            0, SourceFrameSize, SourceFrameSize);
    }
}
