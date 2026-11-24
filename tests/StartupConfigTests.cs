using System.Text.Json;
using RaylibGameFramework.Configuration;
using RaylibGameFramework.Input;
using RaylibGameFramework.Menus;
using RaylibPixelFootball.Application;
using Xunit;

namespace RaylibPixelFootball.Tests;

public sealed class StartupConfigTests
{
    [Fact]
    public void StartupFilesLoadAndMenusReferenceAvailableActions()
    {
        string path = AppContext.BaseDirectory;
        var window = ConfigLoader.Load(Path.Combine(path, "config.json"));
        var game = JsonSerializer.Deserialize<PixelGameConfig>(
            File.ReadAllText(Path.Combine(path, "config.json")));
        var input = InputConfigLoader.Load(Path.Combine(path, "input.json"));
        var menus = MenuConfigLoader.Load(Path.Combine(path, "menu.json"));

        Assert.Equal("Pixel Football", window.WindowTitle);
        Assert.NotNull(game);
        Assert.Equal(1280, game.LogicalWidth);
        Assert.Equal(720, game.LogicalHeight);
        Assert.True(DisplayResolutions.IsWindowOption(window.WindowWidth, window.WindowHeight));
        Assert.True(DisplayResolutions.IsRenderOption(game.LogicalWidth, game.LogicalHeight));
        Assert.Contains("Main", menus.Menus.Keys);
        Assert.Contains("Pause", menus.Menus.Keys);
        Assert.Contains(menus.Menus["Main"].Items, item => item.Function == "StartGame");
        Assert.Contains(menus.Menus["Options"].Items, item => item.Function == "SetDebug");
        var windowSelector = menus.Menus["Options"].Items.Single(item => item.Function == "SetWindowResolution");
        var renderSelector = menus.Menus["Options"].Items.Single(item => item.Function == "SetRenderResolution");
        Assert.Equal(DisplayResolutions.WindowOptions, windowSelector.Options);
        Assert.Equal(DisplayResolutions.RenderOptions, renderSelector.Options);
        Assert.Equal(DisplayResolutions.Label(window.WindowWidth, window.WindowHeight),
            windowSelector.Value.GetString());
        Assert.Equal(DisplayResolutions.Label(game.LogicalWidth, game.LogicalHeight),
            renderSelector.Value.GetString());

        var actions = input.Bindings.Select(binding => binding.Action)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in menus.Menus.Values.SelectMany(menu => menu.Items))
        {
            if (item.Type == "KeyBind")
                Assert.Contains(item.Action!, actions);
            if (item.Function == "OpenMenu")
                Assert.Contains(item.Target!, menus.Menus.Keys);
        }
    }
}
