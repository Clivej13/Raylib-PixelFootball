using System.Numerics;
using RaylibPixelFootball.TwoD;

namespace RaylibPixelFootball.Game;

internal sealed class PlayerEntity
{
    private readonly PlayerSpriteAssets _assets;
    private readonly SpritePlayback _playback;
    private readonly PlayerAnimationKind _baseAnimation;
    private PlayerAnimationKind _animation;
    private float _recoveryRemaining;
    private float _knockdownRemaining;
    private DownEvent _downEvent;
    private bool _pendingTackleRecovery;
    private PileRecoveryQueue? _recoveryQueue;
    private float _momentumRetention = 1f;
    public const int DisplaySize = 12;
    // Yard markings span about 3.7 world units each on the portrait field.
    // Calibrated against a timed goal-line-to-40 run on the displayed artwork.
    public const float WorldUnitsPerYard = 3.7f;
    public const float GameplayPaceMultiplier = 1.03f;
    public const float TopSpeedWorldUnitsPerSecond = 40f * WorldUnitsPerYard / 4.21f * GameplayPaceMultiplier;

    public Vector2 Position { get; private set; }
    public TeamId Team { get; }
    public PlayerAttributes Attributes { get; }
    public int DirectionIndex { get; private set; }
    public int FrameIndex => _playback.FrameIndex;
    public string AnimationName => _animation.ToString();
    public PlayerAnimationKind Animation => _animation;
    public bool HasBall { get; private set; }
    public bool IsDowned { get; private set; }
    public bool BlocksMovement => !IsDowned
        && _animation is not (PlayerAnimationKind.Tackle or PlayerAnimationKind.BlockLoss);
    public float KnockdownRemaining => _knockdownRemaining;
    public float RecoveryRemaining => _recoveryRemaining;
    public float MomentumRetention => _recoveryRemaining > 0f ? _momentumRetention : 1f;

    public PlayerEntity(PlayerSpriteAssets assets, PlayerSpawn spawn,
        PlayerAnimationKind baseAnimation = PlayerAnimationKind.Idle,
        PlayerAttributes? attributes = null)
    {
        if (baseAnimation is not (PlayerAnimationKind.Idle or PlayerAnimationKind.Run))
            throw new ArgumentOutOfRangeException(nameof(baseAnimation));
        _assets = assets;
        _baseAnimation = baseAnimation;
        _animation = baseAnimation;
        Position = spawn.Position;
        DirectionIndex = spawn.DirectionIndex;
        Team = spawn.Team;
        Attributes = attributes ?? new PlayerAttributes();
        _playback = new SpritePlayback(PlayerSpriteAssets.Layout, 10f);
        _playback.Play(DirectionIndex, loop: true, restart: true);
        _playback.Update(spawn.AnimationPhase);
    }

    public void Reset(Vector2 position, int directionIndex = 0)
    {
        PlayerSpriteAssets.Layout.SourceRectangle(directionIndex, 0);
        Position = position;
        DirectionIndex = directionIndex;
        _animation = _baseAnimation;
        HasBall = false;
        IsDowned = false;
        _knockdownRemaining = 0f;
        _pendingTackleRecovery = false;
        _recoveryQueue = null;
        _recoveryRemaining = 0f;
        _momentumRetention = 1f;
        _playback.Play(directionIndex, loop: true, restart: true);
    }

    public void StartAction(PlayerAnimationKind action, bool interrupt = false)
    {
        if (action is PlayerAnimationKind.Idle or PlayerAnimationKind.Run
            or PlayerAnimationKind.RunWithBall)
            throw new ArgumentOutOfRangeException(nameof(action));
        if (IsDowned && action != PlayerAnimationKind.Tackled) return;
        if (IsAction && !interrupt) return;
        _animation = action;
        _playback.Play(DirectionIndex, loop: false, restart: true,
            frameCount: PlayerSpriteAssets.LayoutFor(action).FramesPerAnimation);
    }

    public void SetBallCarrier(bool carrying)
    {
        HasBall = carrying;
        if (_animation is PlayerAnimationKind.Run or PlayerAnimationKind.RunWithBall)
            SetLoop(carrying ? PlayerAnimationKind.RunWithBall : PlayerAnimationKind.Run);
    }

    public void KnockDown(DownEvent cause, float recoverySeconds,
        PlayerAnimationKind fallAnimation = PlayerAnimationKind.Tackled)
    {
        if (!float.IsFinite(recoverySeconds) || recoverySeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(recoverySeconds));
        if (fallAnimation is not (PlayerAnimationKind.Tackled or PlayerAnimationKind.BlockLoss))
            throw new ArgumentOutOfRangeException(nameof(fallAnimation));
        RecoveryPlan plan = RecoveryAnimations.For(cause);
        IsDowned = true;
        _downEvent = cause;
        _pendingTackleRecovery = false;
        _knockdownRemaining = Math.Max(recoverySeconds, plan.MinimumDownSeconds);
        _animation = fallAnimation;
        _playback.Play(DirectionIndex, loop: false, restart: true);
    }

    public void BeginTackleRecovery()
    {
        if (IsDowned) return;
        _pendingTackleRecovery = true;
        StartAction(PlayerAnimationKind.Tackle, interrupt: true);
    }

    public void JoinRecoveryQueue(PileRecoveryQueue queue) =>
        _recoveryQueue = queue;

    public void ApplyCatchControl(ActionConsequence consequence)
    {
        _recoveryRemaining = Math.Max(0f, consequence.RecoverySeconds);
        _momentumRetention = Math.Clamp(consequence.MomentumRetention, 0f, 1f);
    }

    public void MoveEngaged(Vector2 displacement, float fieldTop, float fieldBottom)
    {
        if (!float.IsFinite(displacement.X) || !float.IsFinite(displacement.Y))
            throw new ArgumentOutOfRangeException(nameof(displacement));
        Position = Vector2.Clamp(Position + displacement,
            new Vector2(48f, fieldTop + DisplaySize + DisplaySize / 2f),
            new Vector2(272f, fieldBottom - DisplaySize / 2f));
    }

    public void BeginEngage()
    {
        _animation = PlayerAnimationKind.Engage;
        _playback.Play(DirectionIndex, loop: true, restart: true);
    }

    public void EndEngage()
    {
        if (_animation == PlayerAnimationKind.Engage)
            SetLoop(_baseAnimation);
    }

    public void Update(Vector2 movement, float deltaTime, float fieldTop, float fieldBottom,
        IReadOnlyList<PlayerEntity>? fieldPlayers = null)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        while (deltaTime > 0f)
        {
            float step = Math.Min(deltaTime, 0.05f);
            UpdateStep(movement, step, fieldTop, fieldBottom, fieldPlayers);
            deltaTime -= step;
        }
    }

    private void UpdateStep(Vector2 movement, float deltaTime, float fieldTop, float fieldBottom,
        IReadOnlyList<PlayerEntity>? fieldPlayers)
    {
        if (IsDowned)
        {
            AdvanceKnockdown(deltaTime, fieldPlayers);
            return;
        }
        if (movement.LengthSquared() > 1f)
            movement = Vector2.Normalize(movement);
        movement *= MomentumRetention;
        bool moving = movement.LengthSquared() > 0.001f;

        if (IsAction)
        {
            if (moving && (_animation is PlayerAnimationKind.BullRush
                or PlayerAnimationKind.CatchStationary or PlayerAnimationKind.CatchRac
                or PlayerAnimationKind.CatchHigh or PlayerAnimationKind.CatchDive
                or PlayerAnimationKind.Juke or PlayerAnimationKind.Spin
                or PlayerAnimationKind.Truck or PlayerAnimationKind.StiffArm))
            {
                DirectionIndex = DirectionFromMovement(movement);
                float actionPace = _animation == PlayerAnimationKind.BullRush ? 0.65f : 1f;
                MoveOnField(movement * TopSpeedWorldUnitsPerSecond * deltaTime * actionPace,
                    fieldTop, fieldBottom, fieldPlayers);
                _playback.Play(DirectionIndex, loop: false, preservePhase: true);
            }
            AdvanceActionPlayback(deltaTime,
                moving ? MovementAnimation : PlayerAnimationKind.Idle);
        }
        else
        {
            if (moving)
            {
                DirectionIndex = DirectionFromMovement(movement);
                MoveOnField(movement * TopSpeedWorldUnitsPerSecond * deltaTime,
                    fieldTop, fieldBottom, fieldPlayers);
            }
            SetLoop(moving ? MovementAnimation : PlayerAnimationKind.Idle);
            _playback.Play(DirectionIndex, loop: true, preservePhase: true);
            _playback.Update(deltaTime);
        }
        float halfSize = DisplaySize / 2f;
        Position = Vector2.Clamp(Position,
            new Vector2(48, fieldTop + DisplaySize + halfSize),
            new Vector2(272, fieldBottom - halfSize));
        _recoveryRemaining = Math.Max(0f, _recoveryRemaining - deltaTime);
    }

    private void MoveOnField(Vector2 displacement, float fieldTop, float fieldBottom,
        IReadOnlyList<PlayerEntity>? fieldPlayers)
    {
        Vector2 minimum = new(48f, fieldTop + DisplaySize + DisplaySize / 2f);
        Vector2 maximum = new(272f, fieldBottom - DisplaySize / 2f);
        if (fieldPlayers is null)
        {
            Position = Vector2.Clamp(Position + displacement, minimum, maximum);
            return;
        }
        Vector2[] obstacles = fieldPlayers
            .Where(player => !ReferenceEquals(player, this) && player.BlocksMovement)
            .Select(player => player.Position).ToArray();
        Position = PlayerCollision.Move(Position, displacement, obstacles, minimum, maximum);
    }

    public void AnimateInPlace(float deltaTime,
        IReadOnlyList<PlayerEntity>? fieldPlayers = null)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        while (deltaTime > 0f)
        {
            float step = Math.Min(deltaTime, 0.05f);
            if (IsDowned)
                AdvanceKnockdown(step, fieldPlayers);
            else
            {
                AdvanceActionPlayback(step, _baseAnimation);
            }
            deltaTime -= step;
        }
    }

    private void AdvanceActionPlayback(float deltaTime, PlayerAnimationKind fallback)
    {
        _playback.Update(deltaTime);
        if (!IsAction || !_playback.IsComplete) return;
        if (_pendingTackleRecovery && _animation == PlayerAnimationKind.Tackle)
        {
            _pendingTackleRecovery = false;
            EnterLyingDown(DownEvent.MadeTackle);
        }
        else
            SetLoop(fallback);
    }

    private void EnterLyingDown(DownEvent cause)
    {
        if (!IsDowned)
            _knockdownRemaining = RecoveryAnimations.For(cause).MinimumDownSeconds;
        IsDowned = true;
        _downEvent = cause;
        _animation = PlayerAnimationKind.LyingDown;
        _playback.Play(DirectionIndex, loop: true, restart: true);
    }

    private void AdvanceKnockdown(float deltaTime,
        IReadOnlyList<PlayerEntity>? fieldPlayers)
    {
        _knockdownRemaining = Math.Max(0f, _knockdownRemaining - deltaTime);
        _recoveryRemaining = Math.Max(0f, _recoveryRemaining - deltaTime);
        if (_animation is PlayerAnimationKind.Tackled or PlayerAnimationKind.BlockLoss)
        {
            _playback.Update(deltaTime);
            if (_playback.IsComplete) EnterLyingDown(_downEvent);
            return;
        }
        bool occupied = fieldPlayers is not null && fieldPlayers.Any(player =>
            !ReferenceEquals(player, this) && player.BlocksMovement
            && Vector2.DistanceSquared(player.Position, Position)
                < PlayerCollision.MinimumSeparation * PlayerCollision.MinimumSeparation);
        if (_animation == PlayerAnimationKind.LyingDown)
        {
            _playback.Update(deltaTime);
            if (_knockdownRemaining > 0f || occupied
                || _recoveryQueue?.CanGetUp(this) == false) return;
            _animation = RecoveryAnimations.For(_downEvent).GetUpAnimation;
            _playback.Play(DirectionIndex, loop: false, restart: true);
            return;
        }
        if (occupied || _recoveryQueue?.CanGetUp(this) == false)
        {
            _animation = PlayerAnimationKind.LyingDown;
            _playback.Play(DirectionIndex, loop: true, restart: true);
            return;
        }
        if (_playback.IsComplete)
        {
            IsDowned = false;
            SetLoop(_baseAnimation);
            return;
        }
        _playback.Update(deltaTime * RecoveryAnimations.For(_downEvent).GetUpPlaybackRate);
    }

    public void WalkToSideline(Vector2 destination, float deltaTime,
        float fieldTop, float fieldBottom,
        IReadOnlyList<PlayerEntity> fieldPlayers)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        while (deltaTime > 0f)
        {
            float step = Math.Min(deltaTime, 0.05f);
            deltaTime -= step;
            if (IsDowned)
            {
                AdvanceKnockdown(step, fieldPlayers);
                continue;
            }
            if (IsAction)
            {
                AdvanceActionPlayback(step, _baseAnimation);
                continue;
            }
            Vector2 offset = destination - Position;
            if (offset.LengthSquared() < 0.01f)
            {
                SetLoop(PlayerAnimationKind.Idle);
                _playback.Update(step);
                continue;
            }
            Vector2 movement = Vector2.Normalize(offset);
            DirectionIndex = DirectionFromMovement(movement);
            Vector2 displacement = movement * Math.Min(offset.Length(),
                TopSpeedWorldUnitsPerSecond * 0.7f * step);
            Vector2[] obstacles = fieldPlayers
                .Where(player => !ReferenceEquals(player, this) && player.BlocksMovement)
                .Select(player => player.Position).ToArray();
            Position = PlayerCollision.Move(Position, displacement, obstacles,
                new Vector2(0f, fieldTop - DisplaySize),
                new Vector2(320f, fieldBottom + DisplaySize));
            SetLoop(PlayerAnimationKind.Run);
            _playback.Play(DirectionIndex, loop: true, preservePhase: true);
            _playback.Update(step);
        }
    }

    public void Draw() =>
        _assets.Draw(_animation, Team, DirectionIndex, _playback.FrameIndex, Position, DisplaySize);

    private PlayerAnimationKind MovementAnimation => HasBall
        ? PlayerAnimationKind.RunWithBall : PlayerAnimationKind.Run;

    private bool IsAction => _animation is not (PlayerAnimationKind.Idle
        or PlayerAnimationKind.Run or PlayerAnimationKind.RunWithBall);

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
