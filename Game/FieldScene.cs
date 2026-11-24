using System.Numerics;
using RaylibGameFramework.Input;

namespace RaylibPixelFootball.Game;

// Runs the 97-player load scene over the portrait field artwork.
internal sealed class FieldScene
{
    private PlayerEntity[] _fieldPlayers = [];
    private PlayerEntity[] _sidelinePlayers = [];
    private PlayerEntity? _runner;
    private PlayerEntity? _demoThrower;
    private StadiumArtwork? _artwork;
    private float _demoThrowTimer;
    private float _demoCatchTimer;
    private readonly FieldScroller _scroller;

    public FieldScene(float scrollEdgeBuffer) => _scroller = new FieldScroller(180f, scrollEdgeBuffer);

    public int FieldPlayerCount => _fieldPlayers.Length;
    public int SidelinePlayerCount => _sidelinePlayers.Length;
    public Vector2 PlayerPosition => _runner?.Position ?? new Vector2(160, 105);
    public string PlayerAnimation => _runner?.AnimationName ?? "Idle";
    public int PlayerDirection => _runner?.DirectionIndex ?? 0;
    public int PlayerFrame => _runner?.FrameIndex ?? 0;
    public float CameraTop => _scroller.Top;

    public void AttachArtwork(StadiumArtwork artwork)
    {
        _artwork = artwork;
        _scroller.SetFieldBounds(artwork.GameplayFieldTop, artwork.GameplayFieldBottom);
    }

    public void AttachSprites(PlayerSpriteAssets assets)
    {
        _fieldPlayers = FieldPopulation.CreateField()
            .Select((spawn, index) => new PlayerEntity(assets, spawn.Position,
                spawn.DirectionIndex, spawn.AnimationPhase,
                index == 5 ? PlayerAnimationKind.Idle : PlayerAnimationKind.Run))
            .ToArray();
        _sidelinePlayers = FieldPopulation.CreateSidelines()
            .Select(spawn => new PlayerEntity(assets, spawn.Position,
                spawn.DirectionIndex, spawn.AnimationPhase))
            .ToArray();
        _runner = _fieldPlayers[5];
        _demoThrower = _fieldPlayers[16];
    }

    public void Reset()
    {
        _runner?.Reset(new Vector2(160, 105));
        _scroller.Reset();
        _demoThrowTimer = 0;
        _demoCatchTimer = 0;
    }

    public void Update(InputController input, float deltaTime)
    {
        if (_runner is null) return;
        float x = MathF.Abs(input.GetValue("MoveRight")) - MathF.Abs(input.GetValue("MoveLeft"));
        float y = MathF.Abs(input.GetValue("MoveDown")) - MathF.Abs(input.GetValue("MoveUp"));
        if (input.WasPressed("Throw")) _runner.StartAction(PlayerAnimationKind.Throw);
        else if (input.WasPressed("CatchStationary")) _runner.StartAction(PlayerAnimationKind.CatchStationary);
        else if (input.WasPressed("CatchRac")) _runner.StartAction(PlayerAnimationKind.CatchRac);
        else if (input.WasPressed("CatchHigh")) _runner.StartAction(PlayerAnimationKind.CatchHigh);
        else if (input.WasPressed("CatchDive")) _runner.StartAction(PlayerAnimationKind.CatchDive);
        _runner.Update(new Vector2(x, y), deltaTime,
            _artwork?.GameplayFieldTop ?? 0f, _artwork?.GameplayFieldBottom ?? 180f);
        _scroller.Follow(_runner.Position.Y);

        _demoThrowTimer += Math.Clamp(deltaTime, 0f, 0.05f);
        if (_demoThrowTimer >= 1.5f)
        {
            _demoThrower?.StartAction(PlayerAnimationKind.Throw);
            _demoThrowTimer = 0;
        }
        _demoCatchTimer += Math.Clamp(deltaTime, 0f, 0.05f);
        if (_demoCatchTimer >= 2f)
        {
            _fieldPlayers[12].StartAction(PlayerAnimationKind.CatchStationary);
            _fieldPlayers[14].StartAction(PlayerAnimationKind.CatchRac);
            _fieldPlayers[18].StartAction(PlayerAnimationKind.CatchHigh);
            _fieldPlayers[20].StartAction(PlayerAnimationKind.CatchDive);
            _demoCatchTimer = 0;
        }
        foreach (PlayerEntity player in _fieldPlayers)
            if (!ReferenceEquals(player, _runner))
                player.AnimateInPlace(deltaTime);
        foreach (PlayerEntity player in _sidelinePlayers)
            player.AnimateInPlace(deltaTime);
    }

    public void Draw()
    {
        if (_artwork is null)
            throw new InvalidOperationException("Field artwork must be attached before drawing.");
        _artwork.DrawGameplayField();
        foreach (PlayerEntity player in _sidelinePlayers)
            player.Draw();
        foreach (PlayerEntity player in _fieldPlayers)
            player.Draw();
    }
}
