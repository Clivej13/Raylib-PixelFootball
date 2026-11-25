using System.Buffers.Binary;
using Raylib_cs;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class BallAssetTests
{
    [Fact]
    public void BallSheetHasFortySquareFrames()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Ball",
            "football_ball_animations.png");
        using var stream = File.OpenRead(path);
        byte[] header = new byte[26];
        stream.ReadExactly(header);
        int width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4));
        Assert.Equal(1280, width);
        Assert.Equal(32, height);
        Assert.Equal(8, header[24]);
        Assert.Equal(6, header[25]);

        Image image = Raylib.LoadImage(path);
        try
        {
            Assert.Equal(1280, image.Width);
            Assert.Equal(32, image.Height);
            for (int frame = 0; frame < 40; frame++)
            {
                Image cropped = Raylib.ImageFromImage(image,
                    new Raylib_cs.Rectangle(frame * 32, 0, 32, 32));
                try
                {
                    Assert.Equal(32, cropped.Width);
                    Assert.Equal(32, cropped.Height);
                    bool visible = false;
                    for (int y = 0; y < 32 && !visible; y++)
                    for (int x = 0; x < 32; x++)
                        if (Raylib.GetImageColor(cropped, x, y).A > 0)
                        {
                            visible = true;
                            break;
                        }
                    Assert.True(visible, $"Ball frame {frame + 1} is empty.");
                }
                finally { Raylib.UnloadImage(cropped); }
            }
        }
        finally { Raylib.UnloadImage(image); }
    }
}
