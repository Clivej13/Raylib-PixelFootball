using System.Numerics;
using Raylib_cs;

namespace RaylibPixelFootball.TwoD;

internal readonly record struct ColorSwap(Color Source, Color Target);

// Keeps the source-sheet resize bounded to one directional strip at a time.
internal sealed class SpriteSheet : IDisposable
{
    private readonly Texture2D[,] _textures;
    public SpriteSheetLayout Layout { get; }

    public SpriteSheet(string relativePath, SpriteSheetLayout layout,
        IReadOnlyList<IReadOnlyList<ColorSwap>> palettes)
    {
        Layout = layout;
        if (palettes.Count == 0) throw new ArgumentException("At least one palette is required.", nameof(palettes));
        string path = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException("Sprite sheet is missing.", path);

        Image source = Raylib.LoadImage(path);
        var loaded = new List<Texture2D>(palettes.Count * layout.AnimationCount);
        try
        {
            layout.ValidateImageSize(source.Width, source.Height);
            var textures = new Texture2D[palettes.Count, layout.AnimationCount];
            for (int animation = 0; animation < layout.AnimationCount; animation++)
            {
                Image strip = ExtractNativeStrip(source, layout, animation);
                try
                {
                    for (int paletteIndex = 0; paletteIndex < palettes.Count; paletteIndex++)
                    {
                        Image variant = ApplyPalette(strip, palettes[paletteIndex]);
                        try
                        {
                            Texture2D texture = Raylib.LoadTextureFromImage(variant);
                            if (texture.Id == 0)
                                throw new InvalidDataException($"Could not upload player palette: {path}");
                            loaded.Add(texture);
                            textures[paletteIndex, animation] = texture;
                            Raylib.SetTextureFilter(texture, TextureFilter.Point);
                        }
                        finally
                        {
                            Raylib.UnloadImage(variant);
                        }
                    }
                }
                finally
                {
                    Raylib.UnloadImage(strip);
                }
            }
            _textures = textures;
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

    internal static Image ExtractNativeStrip(Image source, SpriteSheetLayout layout, int animation)
    {
        layout.ValidateImageSize(source.Width, source.Height);
        layout.SourceRectangle(animation, 0);
        int sourceWidth = layout.FramesPerAnimation * layout.SourceFrameSize;
        Image strip = Raylib.ImageFromImage(source,
            new Rectangle(animation * sourceWidth, 0, sourceWidth, layout.SourceFrameSize));
        try
        {
            Raylib.ImageResizeNN(ref strip,
                layout.FramesPerAnimation * layout.FrameSize, layout.FrameSize);
            return strip;
        }
        catch
        {
            Raylib.UnloadImage(strip);
            throw;
        }
    }

    internal static Image ApplyPalette(Image nativeStrip, IReadOnlyList<ColorSwap> palette)
    {
        Image variant = Raylib.ImageCopy(nativeStrip);
        try
        {
            foreach (ColorSwap swap in palette)
                Raylib.ImageColorReplace(ref variant, swap.Source, swap.Target);
            return variant;
        }
        catch
        {
            Raylib.UnloadImage(variant);
            throw;
        }
    }

    public void Draw(int paletteIndex, int animation, int frame, Vector2 feetPosition, int displaySize)
    {
        Layout.SourceRectangle(animation, frame);
        if ((uint)paletteIndex >= (uint)_textures.GetLength(0))
            throw new ArgumentOutOfRangeException(nameof(paletteIndex));
        if (displaySize <= 0) throw new ArgumentOutOfRangeException(nameof(displaySize));
        int size = displaySize;
        var source = new Rectangle(frame * Layout.FrameSize, 0, Layout.FrameSize, Layout.FrameSize);
        var destination = new Rectangle(
            feetPosition.X - size / 2f,
            feetPosition.Y - size,
            size, size);
        Raylib.DrawTexturePro(_textures[paletteIndex, animation], source, destination,
            Vector2.Zero, 0, Color.White);
    }

    public void Dispose()
    {
        foreach (Texture2D texture in _textures)
            if (texture.Id != 0) Raylib.UnloadTexture(texture);
    }
}
