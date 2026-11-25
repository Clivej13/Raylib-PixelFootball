using Raylib_cs;
using RaylibPixelFootball.TwoD;

namespace RaylibPixelFootball.Game;

internal enum TeamId { Home, Away }

internal readonly record struct KitColorOption(string Name, Color Main);

internal static class TeamKits
{
    // These three teal shades are the shirt palette in the imported player sheets.
    private static readonly Color ShirtBase = new(32, 139, 136, 255);
    private static readonly Color ShirtShadow = new(18, 101, 113, 255);
    private static readonly Color ShirtHighlight = new(80, 178, 161, 255);

    public static readonly KitColorOption[] Options =
    [
        new("Blue", new Color(48, 98, 197, 255)),
        new("Red", new Color(192, 55, 67, 255)),
        new("Green", new Color(43, 141, 78, 255)),
        new("Gold", new Color(218, 172, 42, 255)),
        new("White", new Color(229, 234, 226, 255)),
        new("Black", new Color(55, 58, 69, 255))
    ];

    public static int IndexOf(string name)
    {
        for (int i = 0; i < Options.Length; i++)
            if (Options[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return i;
        throw new ArgumentOutOfRangeException(nameof(name), $"Unknown kit color: {name}");
    }

    public static bool IsOption(string? name) =>
        name is not null && Options.Any(option => option.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static ColorSwap[] SwapsFor(int optionIndex)
    {
        Color main = Options[optionIndex].Main;
        return
        [
            new ColorSwap(ShirtBase, main),
            new ColorSwap(ShirtShadow, Shade(main, 0.70f)),
            new ColorSwap(ShirtHighlight, Lighten(main, 0.30f))
        ];
    }

    private static Color Shade(Color color, float factor) =>
        new((int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor), 255);

    private static Color Lighten(Color color, float amount) =>
        new((int)(color.R + (255 - color.R) * amount),
            (int)(color.G + (255 - color.G) * amount),
            (int)(color.B + (255 - color.B) * amount), 255);
}
