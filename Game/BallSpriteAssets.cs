using System.Numerics;
using Raylib_cs;

namespace RaylibPixelFootball.Game;

internal sealed class BallSpriteAssets : IDisposable
{
    public const int FrameSize = 32;
    public const int FrameCount = 40;
    private Texture2D _texture;

    public BallSpriteAssets()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sprites", "Ball",
            "football_ball_animations.png");
        if (!File.Exists(path))
            throw new FileNotFoundException("Ball animation sheet is missing.", path);
        Image image = Raylib.LoadImage(path);
        try
        {
            if (image.Width != FrameSize * FrameCount || image.Height != FrameSize)
                throw new InvalidDataException(
                    $"Ball sheet must be {FrameSize * FrameCount}x{FrameSize}; got {image.Width}x{image.Height}.");
            _texture = Raylib.LoadTextureFromImage(image);
            if (_texture.Id == 0)
                throw new InvalidDataException($"Could not upload ball sheet: {path}");
            Raylib.SetTextureFilter(_texture, TextureFilter.Point);
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    public void Draw(Vector2 position, int frame)
    {
        if ((uint)frame >= FrameCount) throw new ArgumentOutOfRangeException(nameof(frame));
        const float size = 6f;
        Raylib.DrawTexturePro(_texture,
            new Rectangle(frame * FrameSize, 0, FrameSize, FrameSize),
            new Rectangle(position.X - size / 2f, position.Y - size, size, size),
            Vector2.Zero, 0, Color.White);
    }

    public void Dispose()
    {
        if (_texture.Id != 0) Raylib.UnloadTexture(_texture);
        _texture = default;
    }
}
