using System.Numerics;

namespace RaylibPixelFootball.Game;

// Players in a pile recover from the top down. The next player waits until
// the one above has stood and moved clear of their feet.
internal sealed class PileRecoveryQueue
{
    private readonly Dictionary<PlayerEntity, PlayerEntity> _above = [];
    private readonly Dictionary<PlayerEntity, int> _depth = [];

    public void Register(IReadOnlyList<PlayerEntity> bottomToTop)
    {
        if (bottomToTop.Count != bottomToTop.Distinct().Count())
            throw new ArgumentException("A player can occupy only one place in a pile.",
                nameof(bottomToTop));
        for (int index = 0; index < bottomToTop.Count; index++)
        {
            _depth[bottomToTop[index]] = index;
            if (index + 1 < bottomToTop.Count)
                _above[bottomToTop[index]] = bottomToTop[index + 1];
        }
    }

    public bool CanGetUp(PlayerEntity player)
    {
        if (!_above.TryGetValue(player, out PlayerEntity? upper)) return true;
        float clearance = PlayerCollision.MinimumSeparation + 1f;
        return !upper.IsDowned
            && Vector2.DistanceSquared(player.Position, upper.Position)
                >= clearance * clearance;
    }

    public int Depth(PlayerEntity player) =>
        _depth.TryGetValue(player, out int depth) ? depth : 0;

    public void Reset()
    {
        _above.Clear();
        _depth.Clear();
    }
}
