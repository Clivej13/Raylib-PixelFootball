using System.Numerics;

namespace RaylibPixelFootball.Game;

internal readonly record struct PlayerSpawn(Vector2 Position, int DirectionIndex, float AnimationPhase);

internal static class FieldPopulation
{
    public const int FieldCount = 22;
    public const int SidelineCount = 75;

    public static PlayerSpawn[] CreateField()
    {
        var spawns = new PlayerSpawn[FieldCount];
        for (int i = 0; i < FieldCount; i++)
        {
            int column = i % 11;
            bool offense = i < 11;
            spawns[i] = new PlayerSpawn(
                new Vector2(65 + column * 19, offense ? 105 : 75),
                offense ? 0 : 4,
                (i % 8) * 0.1f);
        }
        return spawns;
    }

    public static PlayerSpawn[] CreateSidelines()
    {
        var spawns = new PlayerSpawn[SidelineCount];
        for (int i = 0; i < SidelineCount; i++)
        {
            bool left = i < 38;
            int local = left ? i : i - 38;
            int column = local / 19;
            int row = local % 19;
            float x = left ? 12 + column * 17 : 291 + column * 17;
            float y = 10 + row * 9;
            spawns[i] = new PlayerSpawn(
                new Vector2(x, y),
                left ? 2 : 6,
                (i % 8) * 0.1f);
        }
        return spawns;
    }
}
