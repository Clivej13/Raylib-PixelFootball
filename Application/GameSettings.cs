using System.Text.Json;
using RaylibGameFramework.Configuration;
using RaylibGameFramework.Logging;

namespace RaylibPixelFootball.Application;

public sealed class PixelGameConfig
{
    public int LogicalWidth { get; set; } = 1280;
    public int LogicalHeight { get; set; } = 720;
    public bool DrawGameplayDebug { get; set; }
    public float ScrollEdgeBuffer { get; set; } = 40f;
}

public sealed class LocalSettings
{
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; } = true;
    public bool DrawGameplayDebug { get; set; }
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
    public int? LogicalWidth { get; set; }
    public int? LogicalHeight { get; set; }
}

public sealed class GameSettings
{
    private readonly JsonSettingsStore<LocalSettings> _store;
    private readonly RotatingFileLogger _log;

    public GameSettings(GameConfig window, PixelGameConfig game, RotatingFileLogger log)
    {
        _log = log;
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RaylibPixelFootball");
        _store = new JsonSettingsStore<LocalSettings>(Path.Combine(directory, "settings.json"));
        try
        {
            if (_store.Load() is { } saved)
            {
                window.Fullscreen = saved.Fullscreen;
                window.VSync = saved.VSync;
                game.DrawGameplayDebug = saved.DrawGameplayDebug;
                if (saved.WindowWidth is int windowWidth && saved.WindowHeight is int windowHeight
                    && DisplayResolutions.IsWindowOption(windowWidth, windowHeight))
                {
                    window.WindowWidth = windowWidth;
                    window.WindowHeight = windowHeight;
                }
                if (saved.LogicalWidth is int logicalWidth && saved.LogicalHeight is int logicalHeight
                    && DisplayResolutions.IsRenderOption(logicalWidth, logicalHeight))
                {
                    game.LogicalWidth = logicalWidth;
                    game.LogicalHeight = logicalHeight;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _log.Write("ERROR", "Could not load saved settings; using config.json", exception);
        }
    }

    public void Save(GameConfig window, PixelGameConfig game)
    {
        try
        {
            _store.Save(new LocalSettings
            {
                Fullscreen = window.Fullscreen,
                VSync = window.VSync,
                DrawGameplayDebug = game.DrawGameplayDebug,
                WindowWidth = window.WindowWidth,
                WindowHeight = window.WindowHeight,
                LogicalWidth = game.LogicalWidth,
                LogicalHeight = game.LogicalHeight
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _log.Write("ERROR", "Could not save settings", exception);
        }
    }
}
