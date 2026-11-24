using System.Text.Json;
using RaylibGameFramework.Logging;

namespace RaylibPixelFootball.Application;

public sealed class GameLogConfiguration
{
    public string? Directory { get; set; }
    public long MaxFileBytes { get; set; } = 5 * 1024 * 1024;
    public int RetainedFiles { get; set; } = 5;
}

public static class GameDiagnostics
{
    public static string ResolveDirectory(string baseDirectory, string localAppData, string? configured = null)
    {
        string executableDirectory = Path.GetFullPath(baseDirectory);
        string? repository = null;
        for (var parent = new DirectoryInfo(executableDirectory); parent is not null; parent = parent.Parent)
        {
            if (!File.Exists(Path.Combine(parent.FullName, "RaylibPixelFootball.csproj")))
                continue;
            string relative = Path.GetRelativePath(parent.FullName, executableDirectory);
            string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Length > 1 && parts[0].Equals("bin", StringComparison.OrdinalIgnoreCase)
                && !parts.Any(part => part.Equals("publish", StringComparison.OrdinalIgnoreCase)))
                repository = parent.FullName;
            break;
        }

        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured, repository ?? executableDirectory);
        return repository is not null
            ? Path.Combine(repository, "artifacts", "logs")
            : Path.Combine(localAppData, "RaylibPixelFootball", "Logs");
    }

    public static RotatingFileLogger Create()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string fallbackDirectory = ResolveDirectory(AppContext.BaseDirectory, appData);
        var fallback = new RotatingFileLogger(fallbackDirectory);
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "logging.json");
            var config = File.Exists(path)
                ? JsonSerializer.Deserialize<GameLogConfiguration>(File.ReadAllText(path))
                    ?? throw new JsonException("logging.json must contain an object.")
                : new GameLogConfiguration();
            var logger = new RotatingFileLogger(
                ResolveDirectory(AppContext.BaseDirectory, appData, config.Directory),
                new FileLogOptions
                {
                    MaxFileBytes = config.MaxFileBytes,
                    RetainedFiles = config.RetainedFiles
                });
            logger.Write("INFO", $"Log directory: {logger.DirectoryPath}");
            return logger;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            fallback.Write("ERROR", "Invalid logging configuration; using default logging options", exception);
            return fallback;
        }
    }
}
