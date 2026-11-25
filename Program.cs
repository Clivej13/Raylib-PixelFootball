using System.Text.Json;
using RaylibGameFramework.Configuration;
using RaylibGameFramework.Input;
using RaylibGameFramework.Logging;
using RaylibGameFramework.Menus;
using RaylibPixelFootball.Application;
using RaylibPixelFootball.Game;

var log = GameDiagnostics.Create();
using var crashLogging = new CrashLogging(log);
try
{
    log.Write("INFO", $"Starting Pixel Football; runtime={Environment.Version}; OS={Environment.OSVersion}");
    string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
    GameConfig window = ConfigLoader.Load(configPath);
    PixelGameConfig game = JsonSerializer.Deserialize<PixelGameConfig>(File.ReadAllText(configPath))
        ?? throw new JsonException("config.json must contain an object.");
    if (!DisplayResolutions.IsRenderOption(game.LogicalWidth, game.LogicalHeight))
        throw new InvalidDataException("Unsupported logical resolution in config.json.");
    if (!DisplayResolutions.IsWindowOption(window.WindowWidth, window.WindowHeight))
        throw new InvalidDataException("Unsupported window resolution in config.json.");
    if (!TeamKits.IsOption(game.HomeKitColor) || !TeamKits.IsOption(game.AwayKitColor))
        throw new InvalidDataException("Invalid team kit color in config.json.");
    var settings = new GameSettings(window, game, log);
    var input = InputConfigLoader.Load(Path.Combine(AppContext.BaseDirectory, "input.json"));
    var menus = MenuConfigLoader.Load(Path.Combine(AppContext.BaseDirectory, "menu.json"));
    _ = ActionProfiles.Weights;
    if (args.Contains("--check-config", StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine("Configuration, input, menus, and action weights loaded successfully.");
        return;
    }
    new GameApplication(window, game, input, menus, settings, log).Run();
    log.Write("INFO", "Clean shutdown");
}
catch (Exception exception)
{
    log.Write("FATAL", "Game terminated by managed exception", exception);
    Environment.ExitCode = 1;
}
