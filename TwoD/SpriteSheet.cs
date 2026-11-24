using System.Numerics;
using Raylib_cs;

namespace RaylibPixelFootball.TwoD;

internal sealed class SpriteSheet : IDisposable
{
    private readonly Texture2D[] _textures;
    public SpriteSheetLayout Layout { get; }

    public SpriteSheet(string relativePath, SpriteSheetLayout layout)
    {
        Layout = layout;
        string path = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException("Sprite sheet is missing.", path);

        Image source = Raylib.LoadImage(path);
        var loaded = new List<Texture2D>(layout.AnimationCount);
        try
        {
            layout.ValidateImageSize(source.Width, source.Height);
            for (int animation = 0; animation < layout.AnimationCount; animation++)
            {
                var region = new Rectangle(
                    animation * layout.FramesPerAnimation * layout.SourceFrameSize,
                    0,
                    layout.FramesPerAnimation * layout.SourceFrameSize,
                    layout.SourceFrameSize);
                Image strip = Raylib.ImageFromImage(source, region);
                try
                {
                    Raylib.ImageResizeNN(ref strip,
                        layout.FramesPerAnimation * layout.FrameSize, layout.FrameSize);
                    Texture2D texture = Raylib.LoadTextureFromImage(strip);
                    if (texture.Id == 0)
                        throw new InvalidDataException($"Could not upload sprite strip {animation}: {path}");
                    loaded.Add(texture);
                    Raylib.SetTextureFilter(texture, TextureFilter.Point);
                }
                finally
                {
                    Raylib.UnloadImage(strip);
                }
            }
            _textures = loaded.ToArray();
        }
        catch
        {
            foreach (Texture2D texture in loaded)
                Raylib.UnloadTexture(texture);
            throw;
        }
        finally
        {
            Raylib.UnloadImage(source);
        }
    }

    public void Draw(int animation, int frame, Vector2 feetPosition, int displaySize)
    {
        Layout.SourceRectangle(animation, frame);
        if (displaySize <= 0) throw new ArgumentOutOfRangeException(nameof(displaySize));
        int sourceSize = Layout.FrameSize;
        int size = displaySize;
        var source = new Rectangle(frame * sourceSize, 0, sourceSize, sourceSize);
        var destination = new Rectangle(
            feetPosition.X - size / 2f,
            feetPosition.Y - size,
            size, size);
        Raylib.DrawTexturePro(_textures[animation], source, destination, Vector2.Zero, 0, Color.White);
    }

    public void Dispose()
    {
        foreach (Texture2D texture in _textures)
            Raylib.UnloadTexture(texture);
    }
}
