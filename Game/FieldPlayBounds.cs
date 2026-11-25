using System.Numerics;

namespace RaylibPixelFootball.Game;

internal static class FieldPlayBounds
{
    public const float LeftSideline = 48f;
    public const float RightSideline = 272f;
    // The artwork includes a margin beyond each ten-yard end zone.
    public const float EndZoneInset = 20f + 10f * PlayerEntity.WorldUnitsPerYard;

    public static KickoffEndReason CheckCarrier(Vector2 feet, float fieldTop, float fieldBottom)
    {
        if (feet.X <= LeftSideline || feet.X >= RightSideline)
            return KickoffEndReason.OutOfBounds;
        if (feet.Y <= fieldTop + EndZoneInset || feet.Y >= fieldBottom - EndZoneInset)
            return KickoffEndReason.EndZone;
        return KickoffEndReason.None;
    }
}
