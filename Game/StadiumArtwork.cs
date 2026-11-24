using System.Numerics;
using Raylib_cs;
using RaylibPixelFootball.Application;

namespace RaylibPixelFootball.Game;

internal sealed class StadiumArtwork : IDisposable
{
    private const int SourceScale = 8;
    private Texture2D _skyline;
    private Texture2D _stadium;
    private Texture2D _portraitField;

    public float GameplayFieldTop => (180f - 320f * _portraitField.Height / _portraitField.Width) / 2f;
    public float GameplayFieldBottom => 180f - GameplayFieldTop;

    public StadiumArtwork()
    {
        try
        {
            _skyline = LoadReduced("football_stadium_512_8x.png", 4096, 2304);
            _stadium = LoadReduced("football_stadium_topdown_1024_8x.png", 8192, 4608);
            _portraitField = LoadReduced("football_field_portrait_8x.png", 4096, 6656);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private static Texture2D LoadReduced(string fileName, int expectedWidth, int expectedHeight)
    {
        string path = Path.Combine(AppContext.BaseDirectory,
            "Assets", "Sprites", "Field", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Stadium artwork is missing.", path);
        Image image = Raylib.LoadImage(path);
        try
        {
            if (image.Width != expectedWidth || image.Height != expectedHeight)
                throw new InvalidDataException(
                    $"{fileName} must be {expectedWidth}x{expectedHeight}; got {image.Width}x{image.Height}.");
            Raylib.ImageResizeNN(ref image, expectedWidth / SourceScale, expectedHeight / SourceScale);
            Texture2D texture = Raylib.LoadTextureFromImage(image);
            if (texture.Id == 0)
                throw new InvalidDataException($"Could not upload stadium artwork: {path}");
            Raylib.SetTextureFilter(texture, TextureFilter.Point);
            return texture;
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    public void DrawMenuBackground() => DrawLandscape(_skyline);

    public void DrawPregame(PregameSequence sequence)
    {
        float progress = sequence.StageProgress;
        switch (sequence.Stage)
        {
            case PregameStage.Skyline:
                DrawLandscape(_skyline);
                if (progress > 0.8f) DrawBlack((progress - 0.8f) / 0.2f);
                break;
            case PregameStage.Stadium:
                DrawLandscape(_stadium);
                if (progress < 0.2f) DrawBlack(1f - progress / 0.2f);
                else if (progress > 0.8f) DrawBlack((progress - 0.8f) / 0.2f);
                break;
            case PregameStage.FieldZoom:
                float eased = progress * progress * (3f - 2f * progress);
                DrawFieldZoom(eased);
                if (progress < 0.15f) DrawBlack(1f - progress / 0.15f);
                break;
            case PregameStage.Complete:
                DrawGameplayField();
                break;
        }
    }

    public void DrawGameplayField() => DrawFieldZoom(1f);

    private static void DrawLandscape(Texture2D texture) =>
        Raylib.DrawTexturePro(texture,
            new Rectangle(0, 0, texture.Width, texture.Height),
            new Rectangle(0, 0, 320, 180),
            Vector2.Zero, 0, Color.White);

    private void DrawFieldZoom(float progress)
    {
        const float startHeight = 180f;
        float startWidth = startHeight * _portraitField.Width / _portraitField.Height;
        float width = startWidth + (320f - startWidth) * Math.Clamp(progress, 0f, 1f);
        float height = width * _portraitField.Height / _portraitField.Width;
        var destination = new Rectangle((320f - width) / 2f, (180f - height) / 2f,
            width, height);
        Raylib.DrawTexturePro(_portraitField,
            new Rectangle(0, 0, _portraitField.Width, _portraitField.Height),
            destination, Vector2.Zero, 0, Color.White);
    }

    private static void DrawBlack(float opacity)
    {
        byte alpha = (byte)(Math.Clamp(opacity, 0f, 1f) * 255f);
        Raylib.DrawRectangle(0, 0, 320, 180, new Color((byte)4, (byte)9, (byte)18, alpha));
    }

    public void Dispose()
    {
        if (_portraitField.Id != 0) Raylib.UnloadTexture(_portraitField);
        if (_stadium.Id != 0) Raylib.UnloadTexture(_stadium);
        if (_skyline.Id != 0) Raylib.UnloadTexture(_skyline);
        _portraitField = default;
        _stadium = default;
        _skyline = default;
    }
}
