namespace RaylibPixelFootball.Application;

public static class DisplayResolutions
{
    public static readonly string[] WindowOptions =
        ["640x360", "960x540", "1280x720", "1600x900", "1920x1080", "2560x1440"];

    public static readonly string[] RenderOptions =
        ["320x180", "640x360", "1280x720"];

    public static string Label(int width, int height) => $"{width}x{height}";

    public static bool TryParse(string value, out int width, out int height)
    {
        width = height = 0;
        string[] parts = value.Split('x');
        return parts.Length == 2
            && int.TryParse(parts[0], out width)
            && int.TryParse(parts[1], out height)
            && width > 0 && height > 0;
    }

    public static bool IsWindowOption(int width, int height) =>
        WindowOptions.Contains(Label(width, height));

    public static bool IsRenderOption(int width, int height) =>
        RenderOptions.Contains(Label(width, height));
}
