namespace RaylibPixelFootball.Game;

// Tracks the top edge of a vertical viewport with a player dead zone.
internal sealed class FieldScroller
{
    private readonly float _viewHeight;
    private readonly float _edgeBuffer;
    private float _fieldTop;
    private float _fieldBottom;

    public float Top { get; private set; }

    public FieldScroller(float viewHeight, float edgeBuffer)
    {
        if (!float.IsFinite(viewHeight) || viewHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewHeight));
        if (!float.IsFinite(edgeBuffer) || edgeBuffer < 0 || edgeBuffer >= viewHeight / 2f)
            throw new ArgumentOutOfRangeException(nameof(edgeBuffer));
        _viewHeight = viewHeight;
        _edgeBuffer = edgeBuffer;
        _fieldTop = 0;
        _fieldBottom = viewHeight;
    }

    public void SetFieldBounds(float top, float bottom)
    {
        if (!float.IsFinite(top) || !float.IsFinite(bottom) || bottom - top < _viewHeight)
            throw new ArgumentOutOfRangeException(nameof(bottom));
        _fieldTop = top;
        _fieldBottom = bottom;
        Reset();
    }

    public void Reset() => Top = Math.Clamp(0f, _fieldTop, _fieldBottom - _viewHeight);

    public void Follow(float playerY)
    {
        if (!float.IsFinite(playerY)) throw new ArgumentOutOfRangeException(nameof(playerY));
        float screenY = playerY - Top;
        if (screenY < _edgeBuffer)
            Top = playerY - _edgeBuffer;
        else if (screenY > _viewHeight - _edgeBuffer)
            Top = playerY - (_viewHeight - _edgeBuffer);
        Top = Math.Clamp(Top, _fieldTop, _fieldBottom - _viewHeight);
    }
}
