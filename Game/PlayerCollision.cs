using System.Numerics;

namespace RaylibPixelFootball.Game;

internal static class PlayerCollision
{
    // A circular footprint centred on the sprite's feet; diameter approximates shoulder width.
    public const float MinimumSeparation = 8f;
    private const float MinimumSeparationSquared = MinimumSeparation * MinimumSeparation;

    public static Vector2 Move(Vector2 start, Vector2 displacement,
        IReadOnlyList<Vector2> obstacles, Vector2 minimum, Vector2 maximum)
    {
        Vector2 target = Vector2.Clamp(start + displacement, minimum, maximum);
        Vector2 contact = FurthestClear(start, target, obstacles);
        if (contact == target) return target;

        // Preserve sideways motion after contact, so players can run around bodies.
        Vector2? closest = null;
        float distanceSquared = float.MaxValue;
        foreach (Vector2 obstacle in obstacles)
        {
            float distance = Vector2.DistanceSquared(contact, obstacle);
            if (distance < distanceSquared)
            {
                closest = obstacle;
                distanceSquared = distance;
            }
        }
        if (closest is null) return contact;
        Vector2 normal = contact - closest.Value;
        if (normal.LengthSquared() < 0.000001f) return contact;
        normal = Vector2.Normalize(normal);
        Vector2 remainder = target - contact;
        Vector2 tangent = remainder - Vector2.Dot(remainder, normal) * normal;
        Vector2 slideTarget = Vector2.Clamp(contact + tangent, minimum, maximum);
        return FurthestClear(contact, slideTarget, obstacles);
    }

    private static Vector2 FurthestClear(Vector2 start, Vector2 target,
        IReadOnlyList<Vector2> obstacles)
    {
        Vector2 travel = target - start;
        float lengthSquared = travel.LengthSquared();
        if (lengthSquared < 0.000001f) return start;
        float firstContact = 1f;
        foreach (Vector2 obstacle in obstacles)
        {
            Vector2 offset = start - obstacle;
            float c = offset.LengthSquared() - MinimumSeparationSquared;
            if (c < -0.0001f) return start;
            float b = 2f * Vector2.Dot(offset, travel);
            float discriminant = b * b - 4f * lengthSquared * c;
            if (discriminant <= 0.000001f) continue;
            float root = MathF.Sqrt(discriminant);
            float entry = (-b - root) / (2f * lengthSquared);
            float exit = (-b + root) / (2f * lengthSquared);
            if (exit <= 0f || entry >= firstContact) continue;
            firstContact = Math.Max(0f, entry);
        }
        if (firstContact >= 1f) return target;
        float margin = 0.0001f / MathF.Sqrt(lengthSquared);
        return Vector2.Lerp(start, target, Math.Max(0f, firstContact - margin));
    }

    public static bool Clear(Vector2 position, IReadOnlyList<Vector2> obstacles)
    {
        foreach (Vector2 obstacle in obstacles)
            if (Vector2.DistanceSquared(position, obstacle)
                < MinimumSeparationSquared - 0.0001f)
                return false;
        return true;
    }
}
