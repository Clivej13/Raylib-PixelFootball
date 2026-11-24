using System.Numerics;
using Raylib_cs;

namespace RaylibPixelFootball.TwoD;

// Scenes use a stable 320x180 world; the render target controls how much sprite detail survives.
internal sealed class PixelViewport : IDisposable
{
    private const int WorldWidth = 320;
    private const int WorldHeight = 180;
    private RenderTexture2D _target;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public float Scale { get; private set; }
    public float WorldZoom => Width / (float)WorldWidth;

    public PixelViewport(int width, int height) => Resize(width, height);

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0 || width * WorldHeight != height * WorldWidth)
            throw new ArgumentOutOfRangeException(nameof(width), "Render resolution must be positive and 16:9.");
        if (Width == width && Height == height) return;
        RenderTexture2D replacement = Raylib.LoadRenderTexture(width, height);
        if (replacement.Id == 0)
            throw new InvalidOperationException($"Could not create {width}x{height} render target.");
        Raylib.SetTextureFilter(replacement.Texture, TextureFilter.Point);
        if (_target.Id != 0)
            Raylib.UnloadRenderTexture(_target);
        _target = replacement;
        Width = width;
        Height = height;
    }

    public void Draw(Action drawScene, float cameraTop = 0f)
    {
        Raylib.BeginTextureMode(_target);
        try
        {
            Raylib.ClearBackground(new Color(19, 46, 36, 255));
            Raylib.BeginMode2D(new Camera2D
            {
                Offset = Vector2.Zero,
                Target = new Vector2(0f, cameraTop),
                Rotation = 0,
                Zoom = WorldZoom
            });
            try
            {
                drawScene();
            }
            finally
            {
                Raylib.EndMode2D();
            }
        }
        finally
        {
            Raylib.EndTextureMode();
        }

        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        Scale = MathF.Min(screenWidth / (float)Width, screenHeight / (float)Height);
        var destination = new Rectangle(
            (screenWidth - Width * Scale) / 2f,
            (screenHeight - Height * Scale) / 2f,
            Width * Scale,
            Height * Scale);
        Raylib.DrawTexturePro(_target.Texture,
            new Rectangle(0, 0, Width, -Height),
            destination, Vector2.Zero, 0, Color.White);
    }

    public void Dispose()
    {
        if (_target.Id != 0)
            Raylib.UnloadRenderTexture(_target);
    }
}
