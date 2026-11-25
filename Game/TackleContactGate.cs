namespace RaylibPixelFootball.Game;

// A failed or glancing contact can be retried, but never rerolled every render frame.
internal sealed class TackleContactGate
{
    public static float RetrySeconds =>
        ActionProfiles.Weights.EngagementTiming.TackleRetrySeconds;
    private readonly Dictionary<PlayerEntity, float> _remaining = [];

    public IReadOnlyCollection<PlayerEntity> Excluded => _remaining.Keys;

    public void Mark(PlayerEntity player) => _remaining[player] = RetrySeconds;

    public void Advance(float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        foreach (PlayerEntity player in _remaining.Keys.ToArray())
        {
            float remaining = _remaining[player] - deltaTime;
            if (remaining <= 0.00001f) _remaining.Remove(player);
            else _remaining[player] = remaining;
        }
    }

    public void Reset() => _remaining.Clear();
}
