using System.Numerics;

namespace RaylibPixelFootball.Game;

internal enum BallPossessionState
{
    InFlight, Deflected, Bobbling, UnsecuredPossession,
    SecuringPossession, SecuredPossession, LooseBall, DeadBall
}

internal enum BallPlayType { Kickoff, Pass }
internal enum BallDeadReason { None, IncompletePass, EndOfPlay }
internal enum LooseBallOrigin { None, MuffedKick, Fumble }

internal sealed class BallPossession
{
    public static int MaximumCatchAttempts =>
        ActionProfiles.Weights.BallBehavior.MaximumCatchAttempts;
    private float _stateTimer;
    private float _catchCooldown;
    private float _bobbleWindow;
    private Vector2 _velocity;

    public BallPossessionState State { get; private set; } = BallPossessionState.DeadBall;
    public BallPlayType PlayType { get; private set; }
    public BallDeadReason DeadReason { get; private set; }
    public LooseBallOrigin LooseOrigin { get; private set; }
    public Vector2 Position { get; private set; }
    public int? CarrierId { get; private set; }
    public int CatchAttempts { get; private set; }
    public bool WasPossessed { get; private set; }
    public float RecoveryRemaining => _stateTimer;
    public float MomentumRetention { get; private set; } = 1f;
    public bool IsCarried => State is BallPossessionState.UnsecuredPossession
        or BallPossessionState.SecuringPossession or BallPossessionState.SecuredPossession;
    public bool CanAttemptCatch => CatchAttempts < MaximumCatchAttempts
        && _catchCooldown <= 0f
        && (State is BallPossessionState.InFlight or BallPossessionState.Deflected
            or BallPossessionState.Bobbling or BallPossessionState.LooseBall);

    public void BeginFlight(Vector2 position, BallPlayType playType)
    {
        Position = position;
        PlayType = playType;
        State = BallPossessionState.InFlight;
        DeadReason = BallDeadReason.None;
        LooseOrigin = LooseBallOrigin.None;
        CarrierId = null;
        WasPossessed = false;
        CatchAttempts = 0;
        _stateTimer = 0f;
        _catchCooldown = 0f;
        _bobbleWindow = 0f;
        _velocity = Vector2.Zero;
        MomentumRetention = 1f;
    }

    public void SetFlightPosition(Vector2 position)
    {
        if (State != BallPossessionState.InFlight)
            throw new InvalidOperationException("Ball is not in flight.");
        Position = position;
    }

    public void ApplyCatch(ActionResult result, ActionConsequence consequence,
        int playerId, Vector2 position)
    {
        if (!CanAttemptCatch) throw new InvalidOperationException("Ball is not eligible for a catch.");
        CatchAttempts++;
        Position = position;
        CarrierId = null;
        _stateTimer = consequence.RecoverySeconds;
        MomentumRetention = consequence.MomentumRetention;
        switch (result.Outcome)
        {
            case ActionOutcome.CriticalFailure:
                State = BallPossessionState.Deflected;
                BallBehaviorWeights deflection = ActionProfiles.Weights.BallBehavior;
                _velocity = new Vector2(deflection.DeflectedVelocityX,
                    deflection.DeflectedVelocityY);
                _catchCooldown = deflection.CatchRetryCooldownSeconds;
                break;
            case ActionOutcome.MajorFailure:
                State = BallPossessionState.InFlight;
                BallBehaviorWeights drop = ActionProfiles.Weights.BallBehavior;
                _velocity = new Vector2(0f, drop.DroppedVelocityY);
                _catchCooldown = drop.CatchRetryCooldownSeconds;
                break;
            case ActionOutcome.MinorFailure:
                State = BallPossessionState.Bobbling;
                _catchCooldown = consequence.RecoverySeconds;
                _bobbleWindow = ActionProfiles.Weights.BallBehavior.BobbleWindowSeconds;
                break;
            case ActionOutcome.Neutral:
                SetInitialPossession(BallPossessionState.UnsecuredPossession, playerId);
                break;
            case ActionOutcome.MinorSuccess:
            case ActionOutcome.MajorSuccess:
                SetInitialPossession(BallPossessionState.SecuringPossession, playerId);
                break;
            case ActionOutcome.CriticalSuccess:
                SetInitialPossession(BallPossessionState.SecuredPossession, playerId);
                break;
        }
    }

    private void SetInitialPossession(BallPossessionState state, int playerId)
    {
        State = state;
        CarrierId = playerId;
        WasPossessed = true;
    }

    public void Update(float deltaTime, Vector2? carrierPosition, float groundY)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        _catchCooldown = Math.Max(0f, _catchCooldown - deltaTime);
        if (IsCarried)
        {
            if (carrierPosition is Vector2 carrier)
            {
                BallBehaviorWeights ball = ActionProfiles.Weights.BallBehavior;
                Position = carrier + new Vector2(ball.CarriedOffsetX, ball.CarriedOffsetY);
            }
            _stateTimer = Math.Max(0f, _stateTimer - deltaTime);
            if (_stateTimer <= 0f)
            {
                State = BallPossessionState.SecuredPossession;
                MomentumRetention = 1f;
            }
            return;
        }
        if (State == BallPossessionState.Bobbling)
        {
            float waiting = Math.Min(deltaTime, _stateTimer);
            _stateTimer -= waiting;
            if (_stateTimer <= 0f)
            {
                _bobbleWindow -= deltaTime - waiting;
                if (_bobbleWindow <= 0f || CatchAttempts >= MaximumCatchAttempts)
                    Land();
            }
            return;
        }
        if ((State is BallPossessionState.Deflected or BallPossessionState.InFlight)
            && _velocity != Vector2.Zero)
        {
            Position += _velocity * deltaTime;
            _velocity.Y += ActionProfiles.Weights.BallBehavior.Gravity * deltaTime;
            if (Position.Y >= groundY)
            {
                Position = new Vector2(Position.X, groundY);
                Land();
            }
        }
    }

    public void LandAt(Vector2 position)
    {
        if (State != BallPossessionState.InFlight)
            throw new InvalidOperationException("Only an airborne ball can land.");
        Position = position;
        Land();
    }

    public void LosePossession(Vector2 position)
    {
        if (!IsCarried) throw new InvalidOperationException("No possession to lose.");
        State = BallPossessionState.LooseBall;
        Position = position;
        CarrierId = null;
        _stateTimer = 0f;
        LooseOrigin = LooseBallOrigin.Fumble;
    }

    public void MarkDead(BallDeadReason reason)
    {
        State = BallPossessionState.DeadBall;
        DeadReason = reason;
        CarrierId = null;
    }

    private void Land()
    {
        _velocity = Vector2.Zero;
        CarrierId = null;
        if (PlayType == BallPlayType.Pass && !WasPossessed)
            MarkDead(BallDeadReason.IncompletePass);
        else
        {
            State = BallPossessionState.LooseBall;
            LooseOrigin = WasPossessed ? LooseBallOrigin.Fumble : LooseBallOrigin.MuffedKick;
        }
    }
}
