using System.Numerics;

namespace RaylibPixelFootball.Game;

internal static class PlayerSteering
{
    public const float LookAhead = 30f;
    public const float DetourClearance = PlayerCollision.MinimumSeparation + 3f;

    // Give a defender a waypoint beside the nearest standing blocker in the lane.
    public static Vector2 AroundBlockers(Vector2 position, Vector2 target,
        IReadOnlyList<PlayerEntity> blockers)
    {
        Vector2 route = target - position;
        if (route.LengthSquared() < 0.0001f) return target;
        Vector2 forward = Vector2.Normalize(route);
        Vector2 lateral = new(-forward.Y, forward.X);
        PlayerEntity? nearest = null;
        float nearestForward = float.MaxValue;
        float nearestSide = 0f;
        foreach (PlayerEntity blocker in blockers)
        {
            if (!blocker.BlocksMovement) continue;
            Vector2 offset = blocker.Position - position;
            float ahead = Vector2.Dot(offset, forward);
            float side = Vector2.Dot(offset, lateral);
            if (ahead <= 0f || ahead >= LookAhead
                || MathF.Abs(side) >= DetourClearance || ahead >= nearestForward)
                continue;
            nearest = blocker;
            nearestForward = ahead;
            nearestSide = side;
        }
        if (nearest is null) return target;
        float direction = nearestSide >= 0f ? -1f : 1f;
        return nearest.Position
            + lateral * direction * DetourClearance
            + forward * PlayerCollision.MinimumSeparation;
    }
}
