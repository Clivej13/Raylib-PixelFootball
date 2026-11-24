using System.Buffers.Binary;
using System.Numerics;
using RaylibPixelFootball.Game;
using Raylib_cs;
using RaylibPixelFootball.TwoD;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class SpriteSheetTests
{
    [Fact]
    public void CatalogIncludesEveryPlayerAnimationAndEverySheet()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Players");
        var declared = PlayerSpriteAssets.Definitions.Select(item => item.FileName)
            .OrderBy(name => name).ToArray();
        var present = Directory.GetFiles(directory, "football_*_7_8x.png")
            .Select(Path.GetFileName).OrderBy(name => name).ToArray();
        Assert.Equal(Enum.GetValues<PlayerAnimationKind>().Length, declared.Length);
        Assert.Equal(Enum.GetValues<PlayerAnimationKind>().Length,
            PlayerSpriteAssets.Definitions.Select(item => item.Kind).Distinct().Count());
        Assert.Equal(present, declared);
    }

    public static IEnumerable<object[]> SheetNames =>
        PlayerSpriteAssets.Definitions.Select(definition => new object[] { definition.FileName });

    [Theory]
    [MemberData(nameof(SheetNames))]
    public void PlayerSheetsContainEightDirectionalAnimationsOfEightFrames(string fileName)
    {
        var layout = new SpriteSheetLayout(64, 8, 8, 8);
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Players", fileName);
        using var stream = File.OpenRead(path);
        byte[] header = new byte[26];
        stream.ReadExactly(header);

        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, header[..8]);
        int width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4));
        Assert.Equal(8, header[24]); // 8-bit channels
        Assert.Equal(6, header[25]); // RGBA
        layout.ValidateImageSize(width, height);
        Assert.Equal(64, layout.FrameSize);
        Assert.Equal(8, layout.SourceScale);
        Assert.Equal(8, layout.FramesPerAnimation);
        Assert.Equal(8, layout.AnimationCount);
        Assert.Equal(32768, layout.ExpectedWidth);
        Assert.Equal(512, layout.ExpectedHeight);
        Assert.Equal(0, layout.SourceRectangle(0, 0).X);
        Assert.Equal(32256, layout.SourceRectangle(7, 7).X);
    }

    [Theory]
    [MemberData(nameof(SheetNames))]
    public void RaylibDecodesAndSlicesTheLastDirectionalStrip(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Players", fileName);
        Image image = Raylib.LoadImage(path);
        try
        {
            Assert.Equal(32768, image.Width);
            Assert.Equal(512, image.Height);
            Assert.Equal(0, Raylib.GetImageColor(image, 0, 0).A);
            Image strip = Raylib.ImageFromImage(image, new Raylib_cs.Rectangle(28672, 0, 4096, 512));
            try
            {
                Assert.Equal(4096, strip.Width);
                Assert.Equal(512, strip.Height);
                Raylib.ImageResizeNN(ref strip, 512, 64);
                Assert.Equal(512, strip.Width);
                Assert.Equal(64, strip.Height);
                Assert.Equal(0, Raylib.GetImageColor(strip, 0, 0).A);
            }
            finally
            {
                Raylib.UnloadImage(strip);
            }
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    [Theory]
    [InlineData(0, -1, 0)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 0, 2)]
    [InlineData(1, 1, 3)]
    [InlineData(0, 1, 4)]
    [InlineData(-1, 1, 5)]
    [InlineData(-1, 0, 6)]
    [InlineData(-1, -1, 7)]
    public void MovementSelectsTheMatchingFacingDirection(float x, float y, int expected)
    {
        Assert.Equal(expected, PlayerEntity.DirectionFromMovement(new Vector2(x, y)));
    }

    [Fact]
    public void TurningBetweenDiagonalDirectionsKeepsTheRunCycleInPhase()
    {
        var playback = new SpritePlayback(new SpriteSheetLayout(64, 8, 8, 8), 10);
        playback.Play(0, loop: true, restart: true);
        playback.Update(0.35f);
        Assert.Equal(3, playback.FrameIndex);

        playback.Play(1, loop: true, preservePhase: true);
        Assert.Equal(3, playback.FrameIndex);
        playback.Update(0.05f);
        Assert.Equal(4, playback.FrameIndex);

        playback.Play(0, loop: true, preservePhase: true);
        Assert.Equal(4, playback.FrameIndex);
        playback.Update(0.1f);
        Assert.Equal(5, playback.FrameIndex);
    }

    [Fact]
    public void PlaybackStateIsIndependentPerEntityAndThrowStopsAfterEightFrames()
    {
        var layout = new SpriteSheetLayout(64, 8, 8, 8);
        var runner = new SpritePlayback(layout, 10);
        var thrower = new SpritePlayback(layout, 10);
        runner.Play(2, loop: true, restart: true);
        thrower.Play(5, loop: false, restart: true);

        runner.Update(0.35f);
        Assert.Equal(3, runner.FrameIndex);
        Assert.Equal(0, thrower.FrameIndex);

        thrower.Update(0.8f);
        Assert.Equal(7, thrower.FrameIndex);
        Assert.True(thrower.IsComplete);
        Assert.Equal(3, runner.FrameIndex);

        runner.Update(0.6f);
        Assert.Equal(1, runner.FrameIndex);
        Assert.False(runner.IsComplete);
    }
}
