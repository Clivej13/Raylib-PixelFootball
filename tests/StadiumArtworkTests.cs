using System.Buffers.Binary;
using Raylib_cs;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class StadiumArtworkTests
{
    [Theory]
    [InlineData("football_stadium_512_8x.png", 4096, 2304)]
    [InlineData("football_stadium_topdown_1024_8x.png", 8192, 4608)]
    [InlineData("football_field_portrait_8x.png", 4096, 6656)]
    public void ArtworkDecodesAtExpectedSizeAndReducesEightfold(
        string name, int width, int height)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Field", name);
        using (var stream = File.OpenRead(path))
        {
            byte[] header = new byte[26];
            stream.ReadExactly(header);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, header[..8]);
            Assert.Equal(width, BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)));
            Assert.Equal(height, BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4)));
            Assert.Equal(8, header[24]);
            Assert.Equal(6, header[25]);
        }

        Image image = Raylib.LoadImage(path);
        try
        {
            Assert.Equal(width, image.Width);
            Assert.Equal(height, image.Height);
            Raylib.ImageResizeNN(ref image, width / 8, height / 8);
            Assert.Equal(width / 8, image.Width);
            Assert.Equal(height / 8, image.Height);
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }
}
