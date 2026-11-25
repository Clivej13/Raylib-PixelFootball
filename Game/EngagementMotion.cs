using System.Numerics;

namespace RaylibPixelFootball.Game;

internal static class EngagementMotion
{
    public static Vector2 AllowedShift(Vector2 requested, Vector2 coverage, Vector2 blocker,
        float fieldTop, float fieldBottom, IReadOnlyList<Vector2> others)
    {
        float minY = fieldTop + PlayerEntity.DisplaySize + PlayerEntity.DisplaySize / 2f;
        float maxY = fieldBottom - PlayerEntity.DisplaySize / 2f;
        float x = Math.Clamp(requested.X,
            48f - Math.Min(coverage.X, blocker.X), 272f - Math.Max(coverage.X, blocker.X));
        float y = Math.Clamp(requested.Y,
            minY - Math.Min(coverage.Y, blocker.Y), maxY - Math.Max(coverage.Y, blocker.Y));
        Vector2 allowed = new(x, y);
        const float collisionRadius = PlayerCollision.MinimumSeparation;
        while (allowed.LengthSquared() > 0.0001f)
        {
            bool clear = true;
            foreach (Vector2 other in others)
                if (Vector2.DistanceSquared(coverage + allowed, other) < collisionRadius * collisionRadius
                    || Vector2.DistanceSquared(blocker + allowed, other) < collisionRadius * collisionRadius)
                {
                    clear = false;
                    break;
                }
            if (clear) return allowed;
            allowed *= 0.5f;
        }
        return Vector2.Zero;
    }
}
