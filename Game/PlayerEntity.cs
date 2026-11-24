using System.Numerics;
using RaylibPixelFootball.TwoD;

namespace RaylibPixelFootball.Game;

internal sealed class PlayerEntity
{
    private readonly PlayerSpriteAssets _assets;
    private readonly SpritePlayback _playback;
    private readonly PlayerAnimationKind _baseAnimation;
    private PlayerAnimationKind _animation;
    public const int DisplaySize = 12;
    // Yard markings span about 3.7 world units each on the portrait field.
    // Calibrated against a timed goal-line-to-40 run on the displayed artwork.
    public const float WorldUnitsPerYard = 3.7f;
    public const float GameplayPaceMultiplier = 1.03f;
    public const float TopSpeedWorldUnitsPerSecond = 40f * WorldUnitsPerYard / 4.21f * GameplayPaceMultiplier;

    public Vector2 Position { get; private set; }
    public int DirectionIndex { get; private set; }
    public int FrameIndex => _playback.FrameIndex;
    public string AnimationName => _animation.ToString();
    public PlayerAnimationKind Animation => _animation;

    public PlayerEntity(PlayerSpriteAssets assets, Vector2 position, int directionIndex = 0,
        float animationPhase = 0, PlayerAnimationKind baseAnimation = PlayerAnimationKind.Idle)
    {
        if (baseAnimation is not (PlayerAnimationKind.Idle or PlayerAnimationKind.Run))
            throw new ArgumentOutOfRangeException(nameof(baseAnimation));
        _assets = assets;
        _baseAnimation = baseAnimation;
        _animation = baseAnimation;
        Position = position;
        DirectionIndex = directionIndex;
        _playback = new SpritePlayback(PlayerSpriteAssets.Layout, 10f);
        _playback.Play(directionIndex, loop: true, restart: true);
        _playback.Update(animationPhase);
    }

    public void Reset(Vector2 position)
    {
        Position = position;
        DirectionIndex = 0;
        _animation = _baseAnimation;
        _playback.Play(0, loop: true, restart: true);
    }

    public void StartAction(PlayerAnimationKind action)
    {
        if (action is PlayerAnimationKind.Idle or PlayerAnimationKind.Run)
            throw new ArgumentOutOfRangeException(nameof(action));
        if (IsAction) return;
        _animation = action;
        _playback.Play(DirectionIndex, loop: false, restart: true);
    }

    public void Update(Vector2 movement, float deltaTime, float fieldTop, float fieldBottom)
    {
        deltaTime = Math.Clamp(deltaTime, 0f, 0.05f);
        if (movement.LengthSquared() > 1f)
            movement = Vector2.Normalize(movement);
        bool moving = movement.LengthSquared() > 0.001f;

        if (IsAction)
        {
            _playback.Update(deltaTime);
            if (_playback.IsComplete)
                SetLoop(moving ? PlayerAnimationKind.Run : PlayerAnimationKind.Idle);
        }
        else
        {
            if (moving)
            {
                DirectionIndex = DirectionFromMovement(movement);
                Position += movement * TopSpeedWorldUnitsPerSecond * deltaTime;
            }
            SetLoop(moving ? PlayerAnimationKind.Run : PlayerAnimationKind.Idle);
            _playback.Play(DirectionIndex, loop: true, preservePhase: true);
            _playback.Update(deltaTime);
        }
        float halfSize = DisplaySize / 2f;
        Position = Vector2.Clamp(Position,
            new Vector2(48, fieldTop + halfSize),
            new Vector2(272, fieldBottom - halfSize));
    }

    public void AnimateInPlace(float deltaTime)
    {
        _playback.Update(Math.Clamp(deltaTime, 0f, 0.05f));
        if (IsAction && _playback.IsComplete)
            SetLoop(_baseAnimation);
    }

    public void Draw() =>
        _assets.Get(_animation).Draw(DirectionIndex, _playback.FrameIndex, Position, DisplaySize);

    private bool IsAction => _animation is not (PlayerAnimationKind.Idle or PlayerAnimationKind.Run);

    private void SetLoop(PlayerAnimationKind kind)
    {
        if (_animation == kind) return;
        _animation = kind;
        _playback.Play(DirectionIndex, loop: true, restart: true);
    }

    // The strip starts facing north and rotates clockwise in 45-degree steps.
    public static int DirectionFromMovement(Vector2 movement)
    {
        if (movement.LengthSquared() < 0.001f) return 0;
        float angle = MathF.Atan2(movement.X, -movement.Y);
        int sector = (int)MathF.Round(angle / (MathF.PI / 4f));
        return (sector + 8) % 8;
    }
}
