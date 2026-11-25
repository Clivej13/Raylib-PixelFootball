using System.Text.Json;
using Raylib_cs;
using RaylibGameFramework.Configuration;
using RaylibGameFramework.Input;
using RaylibGameFramework.Logging;
using RaylibGameFramework.Menus;
using RaylibPixelFootball.Game;
using RaylibPixelFootball.TwoD;

namespace RaylibPixelFootball.Application;

internal enum GameState { MainMenu, Pregame, Playing, Paused }

public sealed class GameApplication
{
    private readonly GameConfig _window;
    private readonly PixelGameConfig _gameConfig;
    private readonly GameSettings _settings;
    private readonly RotatingFileLogger _log;
    private readonly InputController _input;
    private readonly InputConfig _inputConfig;
    private readonly MenuConfig _menuConfig;
    private readonly MenuManager _mainMenu;
    private readonly MenuManager _pauseMenu;
    private readonly FieldScene _field;
    private readonly PregameSequence _pregame = new();
    private PixelViewport? _viewport;
    private GameState _state = GameState.MainMenu;
    private bool _exitRequested;

    public GameApplication(GameConfig window, PixelGameConfig gameConfig,
        InputConfig inputConfig, MenuConfig menuConfig, GameSettings settings, RotatingFileLogger log)
    {
        _window = window;
        _gameConfig = gameConfig;
        _field = new FieldScene(gameConfig.ScrollEdgeBuffer);
        _settings = settings;
        _log = log;
        _input = new InputController(inputConfig);
        _inputConfig = inputConfig;
        _menuConfig = menuConfig;
        foreach (var item in menuConfig.Menus.Values.SelectMany(menu => menu.Items))
        {
            if (item.Function == "SetFullscreen")
                item.Value = JsonSerializer.SerializeToElement(window.Fullscreen);
            else if (item.Function == "SetVSync")
                item.Value = JsonSerializer.SerializeToElement(window.VSync);
            else if (item.Function == "SetDebug")
                item.Value = JsonSerializer.SerializeToElement(gameConfig.DrawGameplayDebug);
            else if (item.Function == "SetWindowResolution")
                item.Value = JsonSerializer.SerializeToElement(
                    DisplayResolutions.Label(window.WindowWidth, window.WindowHeight));
            else if (item.Function == "SetRenderResolution")
                item.Value = JsonSerializer.SerializeToElement(
                    DisplayResolutions.Label(gameConfig.LogicalWidth, gameConfig.LogicalHeight));
            else if (item.Function is "SetHomeKitColor" or "SetAwayKitColor")
                item.Value = JsonSerializer.SerializeToElement(item.Function == "SetHomeKitColor"
                    ? gameConfig.HomeKitColor : gameConfig.AwayKitColor);
        }
        _mainMenu = new MenuManager(menuConfig, _input, inputConfig);
        _pauseMenu = new MenuManager(
            new MenuConfig { StartMenu = "Pause", Menus = menuConfig.Menus }, _input, inputConfig);
    }

    public void Run()
    {
        ConfigFlags flags = 0;
        if (_window.VSync) flags |= ConfigFlags.VSyncHint;
        if (_window.Fullscreen) flags |= ConfigFlags.FullscreenMode;
        if (flags != 0) Raylib.SetConfigFlags(flags);
        Raylib.InitWindow(Math.Max(640, _window.WindowWidth),
            Math.Max(360, _window.WindowHeight), _window.WindowTitle);
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(Math.Max(1, _window.TargetFps));
        ConfigureWindowResolutions();
        _log.Write("INFO", $"Window ready: {Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}");
        try
        {
            using var viewport = new PixelViewport(_gameConfig.LogicalWidth, _gameConfig.LogicalHeight);
            _viewport = viewport;
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(4, 9, 18, 255));
            Raylib.DrawText("Loading stadium artwork...", 24, 24, 24, Color.White);
            Raylib.EndDrawing();
            using var artwork = new StadiumArtwork();
            _field.AttachArtwork(artwork);
            using var sprites = new PlayerSpriteAssets(_gameConfig.HomeKitColor, _gameConfig.AwayKitColor);
            _field.AttachSprites(sprites);
            using var ballSprites = new BallSpriteAssets();
            _field.AttachBall(ballSprites);
            _log.Write("INFO", $"Loaded {PlayerSpriteAssets.Definitions.Length} player sheets and the ball animation sheet");
            while (!_exitRequested && !Raylib.WindowShouldClose())
            {
                _input.Update();
                Update(Raylib.GetFrameTime());
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                viewport.Draw(() =>
                {
                    if (_state == GameState.MainMenu) artwork.DrawMenuBackground();
                    else if (_state == GameState.Pregame) artwork.DrawPregame(_pregame);
                    else _field.Draw();
                }, _state is GameState.Playing or GameState.Paused ? _field.CameraTop : 0f);
                if (_state is GameState.MainMenu or GameState.Paused)
                {
                    Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(),
                        new Color(5, 13, 23, 215));
                    (_state == GameState.Paused ? _pauseMenu : _mainMenu).Draw();
                }
                if (_state == GameState.Playing)
                    DrawKickoffStatus();
                if (_gameConfig.DrawGameplayDebug)
                    DrawDebug(viewport);
                Raylib.EndDrawing();
            }
        }
        catch (Exception exception)
        {
            _log.Write("FATAL", $"Game loop failed; state={_state}; menu={ActiveMenuName}", exception);
            throw;
        }
        finally
        {
            _viewport = null;
            Raylib.CloseWindow();
        }
    }

    private string ActiveMenuName => _state == GameState.Paused
        ? _pauseMenu.CurrentMenuName : _mainMenu.CurrentMenuName;

    private void Update(float deltaTime)
    {
        if (!_input.IsRebinding && _input.WasPressed("ToggleDebug"))
            SetDebug(!_gameConfig.DrawGameplayDebug);

        switch (_state)
        {
            case GameState.MainMenu:
                HandleMenu(_mainMenu.Update());
                break;
            case GameState.Pregame:
                if (_input.WasPressed("MenuBack"))
                {
                    _mainMenu.ReturnToStartMenu();
                    ChangeState(GameState.MainMenu);
                }
                else if (_input.WasPressed("MenuConfirm") || _input.WasPressed("Throw"))
                {
                    _pregame.Skip();
                    ChangeState(GameState.Playing);
                }
                else
                {
                    _pregame.Update(deltaTime);
                    if (_pregame.IsComplete) ChangeState(GameState.Playing);
                }
                break;
            case GameState.Playing:
                if (_input.WasPressed("Pause"))
                {
                    _pauseMenu.ReturnToStartMenu();
                    ChangeState(GameState.Paused);
                }
                else
                    _field.Update(_input, deltaTime);
                break;
            case GameState.Paused:
                if (_pauseMenu.CurrentMenuName == "Pause" && _input.WasPressed("Pause"))
                    ChangeState(GameState.Playing);
                else
                    HandleMenu(_pauseMenu.Update());
                break;
        }
    }

    private void HandleMenu(MenuAction? action)
    {
        if (action is null) return;
        _log.Write("INFO", $"Menu action: {action.Function}; value={action.Value}");
        switch (action.Function)
        {
            case "StartGame":
                _field.Reset();
                _pregame.Reset();
                ChangeState(GameState.Pregame);
                break;
            case "Resume":
                ChangeState(GameState.Playing);
                break;
            case "MainMenu":
                _mainMenu.ReturnToStartMenu();
                ChangeState(GameState.MainMenu);
                break;
            case "ExitGame":
                _exitRequested = true;
                break;
            case "SetWindowResolution" when action.Value is string windowResolution:
                SetWindowResolution(windowResolution);
                break;
            case "SetRenderResolution" when action.Value is string renderResolution:
                SetRenderResolution(renderResolution);
                break;
            case "SetFullscreen" when action.Value is bool fullscreen:
                if (_window.Fullscreen != fullscreen)
                {
                    Raylib.ToggleFullscreen();
                    _window.Fullscreen = fullscreen;
                    if (!fullscreen) ApplyWindowSize();
                    _settings.Save(_window, _gameConfig);
                }
                break;
            case "SetVSync" when action.Value is bool vsync:
                if (_window.VSync != vsync)
                {
                    if (vsync) Raylib.SetWindowState(ConfigFlags.VSyncHint);
                    else Raylib.ClearWindowState(ConfigFlags.VSyncHint);
                    _window.VSync = vsync;
                    _settings.Save(_window, _gameConfig);
                }
                break;
            case "SetDebug" when action.Value is bool debug:
                SetDebug(debug);
                break;
            case "SetHomeKitColor" when action.Value is string homeColor:
                SetTeamColor(TeamId.Home, homeColor);
                break;
            case "SetAwayKitColor" when action.Value is string awayColor:
                SetTeamColor(TeamId.Away, awayColor);
                break;
        }
    }

    private MenuItemDefinition FindMenuItem(string function) =>
        _menuConfig.Menus.Values.SelectMany(menu => menu.Items)
            .Single(item => item.Function == function);

    private void ConfigureWindowResolutions()
    {
        int monitorWidth = Raylib.GetMonitorWidth(0);
        int monitorHeight = Raylib.GetMonitorHeight(0);
        var available = DisplayResolutions.WindowOptions
            .Where(option => DisplayResolutions.TryParse(option, out int width, out int height)
                && width <= monitorWidth && height <= monitorHeight)
            .ToList();
        if (available.Count == 0)
            available.Add(DisplayResolutions.WindowOptions[0]);

        MenuItemDefinition item = FindMenuItem("SetWindowResolution");
        item.Options.Clear();
        item.Options.AddRange(available);
        string selected = DisplayResolutions.Label(_window.WindowWidth, _window.WindowHeight);
        if (!available.Contains(selected))
        {
            selected = available[^1];
            DisplayResolutions.TryParse(selected, out int width, out int height);
            _window.WindowWidth = width;
            _window.WindowHeight = height;
            if (!_window.Fullscreen) ApplyWindowSize();
            _settings.Save(_window, _gameConfig);
        }
        item.Value = JsonSerializer.SerializeToElement(selected);
    }

    private void ApplyWindowSize()
    {
        Raylib.SetWindowSize(_window.WindowWidth, _window.WindowHeight);
        var origin = Raylib.GetMonitorPosition(0);
        Raylib.SetWindowPosition(
            (int)origin.X + (Raylib.GetMonitorWidth(0) - _window.WindowWidth) / 2,
            (int)origin.Y + (Raylib.GetMonitorHeight(0) - _window.WindowHeight) / 2);
    }

    private void SetWindowResolution(string choice)
    {
        MenuItemDefinition item = FindMenuItem("SetWindowResolution");
        if (!item.Options.Contains(choice)
            || !DisplayResolutions.TryParse(choice, out int width, out int height))
            return;
        if (_window.WindowWidth == width && _window.WindowHeight == height) return;
        _window.WindowWidth = width;
        _window.WindowHeight = height;
        if (!_window.Fullscreen) ApplyWindowSize();
        _log.Write("INFO", $"Window resolution: {choice}");
        _settings.Save(_window, _gameConfig);
    }

    private void SetRenderResolution(string choice)
    {
        if (!DisplayResolutions.RenderOptions.Contains(choice)
            || !DisplayResolutions.TryParse(choice, out int width, out int height))
            return;
        if (_gameConfig.LogicalWidth == width && _gameConfig.LogicalHeight == height) return;
        _viewport?.Resize(width, height);
        _gameConfig.LogicalWidth = width;
        _gameConfig.LogicalHeight = height;
        _log.Write("INFO", $"Render resolution: {choice}");
        _settings.Save(_window, _gameConfig);
    }

    private void SetTeamColor(TeamId team, string choice)
    {
        if (!TeamKits.IsOption(choice)) return;
        if (team == TeamId.Home) _gameConfig.HomeKitColor = choice;
        else _gameConfig.AwayKitColor = choice;
        _field.SetTeamColors(_gameConfig.HomeKitColor, _gameConfig.AwayKitColor);
        _settings.Save(_window, _gameConfig);
    }

    private void SetDebug(bool enabled)
    {
        if (_gameConfig.DrawGameplayDebug == enabled) return;
        _gameConfig.DrawGameplayDebug = enabled;
        foreach (var item in _menuConfig.Menus.Values.SelectMany(menu => menu.Items))
            if (item.Function == "SetDebug")
                item.Value = JsonSerializer.SerializeToElement(enabled);
        _log.Write("INFO", $"Debug overlay: {enabled}");
        _settings.Save(_window, _gameConfig);
    }

    private void ChangeState(GameState next)
    {
        if (_state == next) return;
        _log.Write("INFO", $"State {_state} -> {next}");
        _state = next;
    }

    private void DrawKickoffStatus()
    {
        int width = Math.Min(Raylib.GetScreenWidth() - 16, 560);
        int y = Raylib.GetScreenHeight() - 44;
        Raylib.DrawRectangle(8, y, width, 36, new Color(3, 10, 17, 220));
        Raylib.DrawText(_field.KickoffStatus, 16, y + 8, 18, Color.White);
    }

    private void DrawDebug(PixelViewport viewport)
    {
        Raylib.DrawRectangle(8, 8, 500, 162, new Color(3, 10, 17, 220));
        Raylib.DrawText($"FPS: {Raylib.GetFPS()}  State: {_state}", 16, 14, 18, Color.White);
        Raylib.DrawText($"Player: {_field.PlayerTeam} at {_field.PlayerPosition.X:0.0}, {_field.PlayerPosition.Y:0.0}",
            16, 36, 18, Color.White);
        Raylib.DrawText($"Logical: {viewport.Width}x{viewport.Height}  Scale: {viewport.Scale:0.##}",
            16, 58, 18, Color.White);
        Raylib.DrawText($"{_field.KickoffPhaseName}: {_field.PlayerAnimation} dir={_field.PlayerDirection} frame={_field.PlayerFrame}",
            16, 80, 18, Color.White);
        if (_state == GameState.Playing)
        {
            Raylib.DrawText($"Ball: {_field.BallStateName}  Recovery: {_field.ReturnerRecovery:0.00}s  Blocks: {_field.ActiveBlockCount}",
                16, 102, 15, Color.White);
            ActionResult? result = _field.LatestActionResult;
            string action = result is null ? "Action: waiting" :
                $"{_field.LatestActionName}: {result.Outcome}  score {result.Score:+0.#;-0.#;0}";
            Raylib.DrawText(action, 16, 124, 15, Color.White);
            string rolls = result is null ?
                $"Camera Y: {_field.CameraTop:0.0}  Edge: {_gameConfig.ScrollEdgeBuffer:0.#}" :
                $"Skill {result.PrimarySkill} vs {result.OpposingSkill}  dice {result.PrimaryRoll}/{result.OpposingRoll}  mod {result.ModifierTotal:+0;-0;0}";
            Raylib.DrawText(rolls, 16, 146, 15, Color.White);
        }
        else
        {
            string detail = _state == GameState.Pregame
                ? $"Intro: {_pregame.Stage} {_pregame.Elapsed:0.0}/{PregameSequence.TotalSeconds:0.0}s"
                : $"Menu: {ActiveMenuName}";
            Raylib.DrawText(detail, 16, 102, 18, Color.White);
            Raylib.DrawText($"Entities: {_field.FieldPlayerCount}+{_field.SidelinePlayerCount}",
                16, 124, 18, Color.White);
            Raylib.DrawText($"Camera Y: {_field.CameraTop:0.0}  Edge buffer: {_gameConfig.ScrollEdgeBuffer:0.#}",
                16, 146, 18, Color.White);
        }
        DrawDebugBindings();
    }

    private void DrawDebugBindings()
    {
        (string Action, string Label)[] actions =
        [
            ("MoveUp", "Move Up"), ("MoveDown", "Move Down"),
            ("MoveLeft", "Move Left"), ("MoveRight", "Move Right"),
            ("Throw", "Throw"), ("CatchStationary", "Catch Still"),
            ("CatchRac", "Catch & Run"), ("CatchHigh", "Catch High"),
            ("CatchDive", "Catch Dive"), ("Kick", "Kick"),
            ("BullRush", "Bull Rush"), ("Engage", "Engage"),
            ("BlockLoss", "Block Loss"), ("Pause", "Pause"),
            ("ToggleDebug", "Debug")
        ];
        const int rows = 8;
        Raylib.DrawRectangle(8, 180, Math.Min(624, Raylib.GetScreenWidth() - 16), 130,
            new Color(3, 10, 17, 220));
        Raylib.DrawText("CONTROLS  KEYBOARD / CONTROLLER", 16, 184, 14, Color.White);
        for (int index = 0; index < actions.Length; index++)
        {
            var (action, label) = actions[index];
            int x = index < rows ? 16 : 322;
            int y = 202 + index % rows * 13;
            Raylib.DrawText($"{label}: {DebugBinding(action)}", x, y, 11, Color.White);
        }
    }

    private string DebugBinding(string action)
    {
        string keyboard = string.Join("/", _inputConfig.Bindings
            .Where(binding => binding.Action == action && binding.Device.ToString() == "Keyboard")
            .Select(binding => ShortInput(binding.Input.ToString())));
        string controller = string.Join("/", _inputConfig.Bindings
            .Where(binding => binding.Action == action && binding.Device.ToString() != "Keyboard")
            .Select(binding => ShortInput(binding.Input.ToString())));
        return controller.Length == 0 ? keyboard : $"{keyboard} / {controller}";
    }

    private static string ShortInput(string input) => input switch
    {
        "RightFaceDown" => "A", "RightFaceLeft" => "X",
        "RightFaceRight" => "B", "RightFaceUp" => "Y",
        "LeftTrigger2" => "LT", "RightTrigger2" => "RT",
        "LeftTrigger1" => "LB", "RightTrigger1" => "RB",
        "MiddleLeft" => "Select", "MiddleRight" => "Start",
        "LeftYNegative" => "LS Up", "LeftYPositive" => "LS Down",
        "LeftXNegative" => "LS Left", "LeftXPositive" => "LS Right",
        _ => input
    };
}
