using System.Numerics;

namespace RaylibPixelFootball.Game;

internal abstract class KickoffAgent
{
    private float _recoverySeconds;
    private float _reengageSeconds;

    protected KickoffAgent(PlayerEntity player) => Player = player;

    public PlayerEntity Player { get; }
    public Vector2 Position => Player.Position;
    public bool IsEngaged { get; private set; }
    public bool CanEngage => !Player.IsDowned && !IsEngaged
        && _recoverySeconds <= 0f && _reengageSeconds <= 0f
        && Player.Animation is PlayerAnimationKind.Idle or PlayerAnimationKind.Run;
    public bool CanTackle => !Player.IsDowned && !IsEngaged && _recoverySeconds <= 0f
        && Player.Animation is PlayerAnimationKind.Idle or PlayerAnimationKind.Run
            or PlayerAnimationKind.BullRush;

    public void Reset()
    {
        IsEngaged = false;
        _recoverySeconds = 0f;
        _reengageSeconds = 0f;
    }

    public void Update(float deltaTime, Vector2 carrier, KickoffAgent? opponent,
        float fieldTop, float fieldBottom, IReadOnlyList<PlayerEntity> fieldPlayers)
    {
        _recoverySeconds = Math.Max(0f, _recoverySeconds - deltaTime);
        _reengageSeconds = Math.Max(0f, _reengageSeconds - deltaTime);
        if (IsEngaged || _recoverySeconds > 0f || Player.IsDowned)
        {
            Player.AnimateInPlace(deltaTime, fieldPlayers);
            return;
        }

        Vector2 target = ChooseTarget(carrier, opponent);
        Vector2 offset = target - Position;
        Vector2 movement = offset.LengthSquared() > 4f
            ? Vector2.Normalize(offset) : Vector2.Zero;
        Player.Update(movement, deltaTime, fieldTop, fieldBottom, fieldPlayers);
    }

    protected abstract Vector2 ChooseTarget(Vector2 carrier, KickoffAgent? opponent);
    protected abstract DownEvent LossCause { get; }

    public void BeginEngagement()
    {
        if (!CanEngage) throw new InvalidOperationException("Player cannot start a block yet.");
        IsEngaged = true;
        Player.BeginEngage();
    }

    public void FinishEngagement(bool won, float recoverySeconds)
    {
        if (!IsEngaged) throw new InvalidOperationException("Player is not engaged.");
        IsEngaged = false;
        if (won) Player.StartAction(PlayerAnimationKind.BullRush, interrupt: true);
        else Player.KnockDown(LossCause, recoverySeconds);
        EngagementTimingWeights timing = ActionProfiles.Weights.EngagementTiming;
        _recoverySeconds = won ? timing.WinnerRecoverySeconds
            : Math.Max(timing.LoserMinimumRecoverySeconds, recoverySeconds);
        _reengageSeconds = won ? timing.WinnerReengageSeconds
            : _recoverySeconds + timing.LoserReengageDelaySeconds;
    }

    public void ReleaseEngagement()
    {
        if (!IsEngaged) return;
        IsEngaged = false;
        Player.EndEngage();
        _reengageSeconds = ActionProfiles.Weights.EngagementTiming.ReleasedReengageSeconds;
    }
}

internal sealed class CoverageAgent : KickoffAgent
{
    private readonly IReadOnlyList<PlayerEntity> _blockers;

    public CoverageAgent(PlayerEntity player, IReadOnlyList<PlayerEntity>? blockers = null)
        : base(player) => _blockers = blockers ?? [];

    protected override DownEvent LossCause => DownEvent.LostPassRush;

    protected override Vector2 ChooseTarget(Vector2 carrier, KickoffAgent? opponent) =>
        PlayerSteering.AroundBlockers(Position, carrier, _blockers);
}

internal sealed class BlockerAgent : KickoffAgent
{
    private readonly float _laneX;
    public const float LaneTravel = 4f;

    public BlockerAgent(PlayerEntity player) : base(player) => _laneX = player.Position.X;

    protected override DownEvent LossCause => DownEvent.LostBlock;

    protected override Vector2 ChooseTarget(Vector2 carrier, KickoffAgent? opponent) =>
        opponent is null ? Position : new Vector2(
            Math.Clamp(opponent.Position.X, _laneX - LaneTravel, _laneX + LaneTravel),
            opponent.Position.Y);
}
