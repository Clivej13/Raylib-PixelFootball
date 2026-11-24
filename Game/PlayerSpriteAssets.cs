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
    CatchDive
}

internal sealed class PlayerSpriteAssets : IDisposable
{
    public static SpriteSheetLayout Layout { get; } = new(
        frameSize: 64, sourceScale: 8, framesPerAnimation: 8, animationCount: 8);

    internal static readonly (PlayerAnimationKind Kind, string FileName)[] Definitions =
    [
        (PlayerAnimationKind.Idle, "football_idle_7_8x.png"),
        (PlayerAnimationKind.Run, "football_run_7_8x.png"),
        (PlayerAnimationKind.Throw, "football_throw_7_8x.png"),
        (PlayerAnimationKind.CatchStationary, "football_catch_stationary_7_8x.png"),
        (PlayerAnimationKind.CatchRac, "football_catch_rac_7_8x.png"),
        (PlayerAnimationKind.CatchHigh, "football_catch_high_7_8x.png"),
        (PlayerAnimationKind.CatchDive, "football_catch_dive_7_8x.png")
    ];

    private readonly Dictionary<PlayerAnimationKind, SpriteSheet> _sheets = [];

    public PlayerSpriteAssets()
    {
        try
        {
            foreach (var (kind, fileName) in Definitions)
                _sheets.Add(kind, new SpriteSheet(
                    Path.Combine("Assets", "Sprites", "Players", fileName), Layout));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public SpriteSheet Get(PlayerAnimationKind kind) => _sheets[kind];

    public void Dispose()
    {
        foreach (SpriteSheet sheet in _sheets.Values)
            sheet.Dispose();
        _sheets.Clear();
    }
}
