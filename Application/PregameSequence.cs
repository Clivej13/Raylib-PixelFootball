namespace RaylibPixelFootball.Application;

internal enum PregameStage
{
    Skyline,
    Stadium,
    FieldZoom,
    Complete
}

internal sealed class PregameSequence
{
    public const float SkylineSeconds = 2f;
    public const float StadiumSeconds = 2f;
    public const float FieldZoomSeconds = 2f;
    public const float TotalSeconds = SkylineSeconds + StadiumSeconds + FieldZoomSeconds;

    public float Elapsed { get; private set; }
    public bool IsComplete => Elapsed >= TotalSeconds;

    public PregameStage Stage => Elapsed switch
    {
        < SkylineSeconds => PregameStage.Skyline,
        < SkylineSeconds + StadiumSeconds => PregameStage.Stadium,
        < TotalSeconds => PregameStage.FieldZoom,
        _ => PregameStage.Complete
    };

    public float StageProgress => Stage switch
    {
        PregameStage.Skyline => Elapsed / SkylineSeconds,
        PregameStage.Stadium => (Elapsed - SkylineSeconds) / StadiumSeconds,
        PregameStage.FieldZoom => (Elapsed - SkylineSeconds - StadiumSeconds) / FieldZoomSeconds,
        _ => 1f
    };

    public void Reset() => Elapsed = 0;

    public void Skip() => Elapsed = TotalSeconds;

    public void Update(float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        Elapsed = Math.Min(TotalSeconds, Elapsed + deltaTime);
    }
}
