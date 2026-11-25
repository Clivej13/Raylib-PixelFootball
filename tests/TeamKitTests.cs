using Raylib_cs;
using RaylibPixelFootball.Game;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class TeamKitTests
{
    [Fact]
    public void EachTeamGetsElevenStartersAndItsOwnSideline()
    {
        PlayerSpawn[] field = FieldPopulation.CreateField();
        PlayerSpawn[] sideline = FieldPopulation.CreateSidelines();

        Assert.Equal(11, field.Count(player => player.Team == TeamId.Home));
        Assert.Equal(11, field.Count(player => player.Team == TeamId.Away));
        Assert.Equal(38, sideline.Count(player => player.Team == TeamId.Home));
        Assert.Equal(37, sideline.Count(player => player.Team == TeamId.Away));
    }

    public static IEnumerable<object[]> PlayerSheets =>
        PlayerSpriteAssets.Definitions.Select(definition => new object[] { definition.FileName });

    [Theory]
    [MemberData(nameof(PlayerSheets))]
    public void EveryLoadedAnimationContainsTheReplaceableShirtPalette(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Players", fileName);
        Image source = Raylib.LoadImage(path);
        try
        {
            Image frame = Raylib.ImageFromImage(source, new Raylib_cs.Rectangle(0, 0, 512, 512));
            try
            {
                Raylib.ImageResizeNN(ref frame, 64, 64);
                Assert.True(FindPixel(frame, new Color(32, 139, 136, 255)).x >= 0,
                    $"{fileName} has no replaceable jersey pixels.");
            }
            finally { Raylib.UnloadImage(frame); }
        }
        finally { Raylib.UnloadImage(source); }
    }

    [Fact]
    public void ShirtPaletteChangesWhileSkinStaysTheSame()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Players",
            "football_idle_7_8x.png");
        Image source = Raylib.LoadImage(path);
        try
        {
            Image frame = Raylib.ImageFromImage(source, new Raylib_cs.Rectangle(0, 0, 512, 512));
            try
            {
                Raylib.ImageResizeNN(ref frame, 64, 64);
                var shirt = new Color(32, 139, 136, 255);
                var skin = new Color(255, 240, 198, 255);
                (int x, int y) shirtPixel = FindPixel(frame, shirt);
                (int x, int y) skinPixel = FindPixel(frame, skin);
                Assert.True(shirtPixel.x >= 0);
                Assert.True(skinPixel.x >= 0);

                foreach (var swap in TeamKits.SwapsFor(TeamKits.IndexOf("Blue")))
                    Raylib.ImageColorReplace(ref frame, swap.Source, swap.Target);

                Color changed = Raylib.GetImageColor(frame, shirtPixel.x, shirtPixel.y);
                Color unchanged = Raylib.GetImageColor(frame, skinPixel.x, skinPixel.y);
                Assert.Equal(TeamKits.Options[TeamKits.IndexOf("Blue")].Main, changed);
                Assert.Equal(skin, unchanged);
            }
            finally { Raylib.UnloadImage(frame); }
        }
        finally { Raylib.UnloadImage(source); }
    }

    private static (int x, int y) FindPixel(Image image, Color expected)
    {
        for (int y = 0; y < image.Height; y++)
        for (int x = 0; x < image.Width; x++)
            if (Raylib.GetImageColor(image, x, y).Equals(expected))
                return (x, y);
        return (-1, -1);
    }
}
