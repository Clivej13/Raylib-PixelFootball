# Pixel Football

A .NET 9 / Raylib starter for a 2D pixel football game. The current scene is a player rendering and movement playground, not a football rules implementation.

## Run

```text
dotnet run
```

Start runs a six-second pre-game sequence: skyline stadium view, overhead stadium view, then a zoom into the portrait field. Enter or controller A skips to gameplay; Escape or controller B returns to the main menu. Gameplay uses the final portrait-field crop as its background, with 22 players in the field and 75 along the left and right sidelines. Each player occupies 12 × 12 field units, which becomes 48 × 48 render pixels at the default resolution. Move one field player with WASD, arrow keys, or a controller left stick. Space or controller A throws. Q/E/R/F trigger stationary, catch-and-run, high, and diving catches; the controller uses X/B/Y/LT. Other field players demonstrate those catches, while sideline players use the idle animation. Every player keeps an independent animation clock. Escape or controller Start pauses during gameplay. The menu supports keyboard, controller, and mouse navigation. Controls can be rebound for the current session.

```text
dotnet build
dotnet test tests/PixelFootball.Tests.csproj
dotnet run -- --check-config
```

The configuration check loads the window, input, and menu files without opening a window. The controlled player moves at up to about 36.2 field units per second. The marked yard spacing was calibrated from a goal-line-to-40 run; a 3% gameplay pace adjustment puts that run at roughly 4.09 seconds at constant speed. During gameplay, the camera stays still until the controlled player enters the top or bottom screen buffer, then follows along the portrait field. Set `ScrollEdgeBuffer` in `config.json` to adjust that buffer in 320×180 field units (default: 40; valid range: 0 to less than 90). The camera stops at the artwork edges and resets for a new game.

## Configuration

- `config.json`: window size, VSync, fullscreen, target FPS, 1280×720 default render resolution, and default debug overlay state.
- `input.json`: game and menu bindings.
- `menu.json`: main, options, controls, and pause menus.
- `logging.json`: rotating log location and limits.

Options offers window resolutions from 640×360 through 2560×1440, filtered to fit the primary monitor, plus 320×180, 640×360, and 1280×720 render resolutions. The 1280×720 render mode preserves more of the 64×64 source art at a 1280×720 window size. The field composition stays fixed at 320×180 world units and scales to larger render and window resolutions. Window and render resolutions, fullscreen, VSync, and debug overlay choices are saved to `%LOCALAPPDATA%/RaylibPixelFootball/settings.json` and override the defaults on later launches. Control rebinding currently lasts for the session.

The project consumes the sibling Configuration, Input, and Menus packages from `../Packages`, and references the sibling Logging project. The 2D viewport and sprite layout/playback code stay in this repository. `TwoD/PixelViewport.cs` owns the render target, scales the 320×180 field coordinates to the selected render resolution, and fits the result to the window. `TwoD/SpriteSheet.cs` slices the seven imported strips into directional textures and reduces their 8× source pixels to native frame size before GPU upload. Both are kept separate from game rules so they can later move to a shared library.

## Debugging

Enable **Debug Overlay** in Options or press F3. It shows FPS, game state, player position, logical resolution, scale, current menu, and field/sideline entity counts. The 97-player scene is a visual load test; an interactive run is needed to measure graphics performance on the target machine. Startup, menu actions, state transitions, settings failures, managed exceptions, and clean shutdown are written to rotating logs. Development builds under this repository use `artifacts/logs`; published builds use `%LOCALAPPDATA%/RaylibPixelFootball/Logs`. `logging.json` can override the directory and rotation limits.
