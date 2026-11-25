using System.Numerics;
using RaylibPixelFootball.TwoD;

namespace RaylibPixelFootball.Game;

internal enum PlayerAnimationKind
{
    Idle,
    Run,
    Throw,
    CatchStationary,
    CatchRac,
    CatchHigh,
    CatchDive,
    Kick,
    BullRush,
    Engage,
    BlockLoss,
    RunWithBall,
    Tackle,
    Tackled,
    LyingDown,
    GetUpFast,
    GetUpHurt,
    GetUpSlow,
    Juke,
    Spin,
    Truck,
    StiffArm
}

internal sealed class PlayerSpriteAssets : IDisposable
{
    public static SpriteSheetLayout Layout { get; } = new(
        frameSize: 64, sourceScale: 8, framesPerAnimation: 8, animationCount: 8);
    public static SpriteSheetLayout KickLayout { get; } = new(
        frameSize: 64, sourceScale: 8, framesPerAnimation: 6, animationCount: 8);

    public static SpriteSheetLayout LayoutFor(PlayerAnimationKind kind) =>
        kind == PlayerAnimationKind.Kick ? KickLayout : Layout;

    internal static readonly (PlayerAnimationKind Kind, string FileName)[] Definitions =
    [
        (PlayerAnimationKind.Idle, "football_idle_7_8x.png"),
        (PlayerAnimationKind.Run, "football_run_7_8x.png"),
        (PlayerAnimationKind.Throw, "football_throw_7_8x.png"),
        (PlayerAnimationKind.CatchStationary, "football_catch_stationary_7_8x.png"),
        (PlayerAnimationKind.CatchRac, "football_catch_rac_7_8x.png"),
        (PlayerAnimationKind.CatchHigh, "football_catch_high_7_8x.png"),
        (PlayerAnimationKind.CatchDive, "football_catch_dive_7_8x.png"),
        (PlayerAnimationKind.Kick, "football_kick_7_8x.png"),
        (PlayerAnimationKind.BullRush, "football_bull_rush_pancake_7_8x.png"),
        (PlayerAnimationKind.Engage, "football_engage_7_8x.png"),
        (PlayerAnimationKind.BlockLoss, "football_block_loss_7_8x.png"),
        (PlayerAnimationKind.RunWithBall, "football_run_with_ball_7_8x.png"),
        (PlayerAnimationKind.Tackle, "football_tackle_7_8x.png"),
        (PlayerAnimationKind.Tackled, "football_tackled_7_8x.png"),
        (PlayerAnimationKind.LyingDown, "football_lying_down_7_8x.png"),
        (PlayerAnimationKind.GetUpFast, "football_get_up_fast_7_8x.png"),
        (PlayerAnimationKind.GetUpHurt, "football_get_up_hurt_7_8x.png"),
        (PlayerAnimationKind.GetUpSlow, "football_get_up_slow_7_8x.png"),
        (PlayerAnimationKind.Juke, "football_runner_juke_7_8x.png"),
        (PlayerAnimationKind.Spin, "football_runner_spin_7_8x.png"),
        (PlayerAnimationKind.Truck, "football_runner_truck_7_8x.png"),
        (PlayerAnimationKind.StiffArm, "football_runner_stiff_arm_7_8x.png")
    ];

    private readonly Dictionary<PlayerAnimationKind, SpriteSheet> _sheets = [];
    private int _homePalette;
    private int _awayPalette;

    public PlayerSpriteAssets(string homeColor, string awayColor)
    {
        SetColors(homeColor, awayColor);
        IReadOnlyList<ColorSwap>[] palettes = TeamKits.Options
            .Select((_, index) => (IReadOnlyList<ColorSwap>)TeamKits.SwapsFor(index))
            .ToArray();
        try
        {
            foreach (var (kind, fileName) in Definitions)
                _sheets.Add(kind, new SpriteSheet(
                    Path.Combine("Assets", "Sprites", "Players", fileName), LayoutFor(kind), palettes));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void SetColors(string homeColor, string awayColor)
    {
        _homePalette = TeamKits.IndexOf(homeColor);
        _awayPalette = TeamKits.IndexOf(awayColor);
    }

    public void Draw(PlayerAnimationKind animation, TeamId team, int direction, int frame,
        Vector2 position, int displaySize)
    {
        _sheets[animation].Draw(PaletteFor(team), direction, frame, position, displaySize);
    }

    private int PaletteFor(TeamId team) => team == TeamId.Home ? _homePalette : _awayPalette;

    public void Dispose()
    {
        foreach (SpriteSheet sheet in _sheets.Values)
            sheet.Dispose();
        _sheets.Clear();
    }
}
