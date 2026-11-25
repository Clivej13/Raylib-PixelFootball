using System.Numerics;

namespace RaylibPixelFootball.Game;

internal enum KickoffPhase { Windup, Flight, Bounce, Loose, Return, Dead }
internal enum KickoffEndReason { None, OutOfBounds, EndZone, Tackled }

internal sealed class KickoffPlay
{
    public static float WindupSeconds => ActionProfiles.Weights.Kickoff.WindupSeconds;
    public static float FlightSeconds => ActionProfiles.Weights.Kickoff.FlightSeconds;
    public static float CatchRadius => ActionProfiles.Weights.Kickoff.CatchRadius;
    private static float[] BounceFrameSeconds =>
        ActionProfiles.Weights.Kickoff.BounceFrameSeconds;
    private readonly BallPossession _possession = new();
    private readonly CatchHandler _catchHandler;
    private PlayerAttributes _returnerAttributes = PlayerAttributes.KickoffReturnTeam(0);
    private Vector2 _origin;
    private Vector2 _landing;
    private float _elapsed;
    private float _looseTime;
    private int _bounceStartFrame = 16;

    public KickoffPlay(ActionResolver? resolver = null, ActionConfig? catchProfile = null) =>
        _catchHandler = new CatchHandler(resolver ?? new ActionResolver(), catchProfile);

    public KickoffPhase Phase { get; private set; } = KickoffPhase.Windup;
    public KickoffEndReason EndReason { get; private set; }
    public BallPossession Ball => _possession;
    public ActionResult? LastCatchResult { get; private set; }
    public Vector2 BallPosition { get; private set; }
    public Vector2 LandingPosition => _landing;
    public Vector2 CameraTarget => BallPosition;
    public int BallFrame { get; private set; }
    public string Status => Phase switch
    {
        KickoffPhase.Windup => "KICKOFF  |  Kicker winding up",
        KickoffPhase.Flight => "KICKOFF  |  Track the ball to the returner",
        KickoffPhase.Bounce when _possession.State == BallPossessionState.Bobbling =>
            "BOBBLE  |  Stay near the ball",
        KickoffPhase.Bounce when _possession.State == BallPossessionState.Deflected =>
            "DEFLECTION  |  Ball still live",
        KickoffPhase.Bounce => "LOOSE BALL  |  Run to recover it",
        KickoffPhase.Loose => "LOOSE BALL  |  Run to recover it",
        KickoffPhase.Return when _possession.State == BallPossessionState.UnsecuredPossession =>
            "UNSECURED  |  Protect the ball",
        KickoffPhase.Return when _possession.State == BallPossessionState.SecuringPossession =>
            "SECURING  |  Finish the catch",
        KickoffPhase.Return => "RETURN  |  Run downfield and avoid tacklers",
        KickoffPhase.Dead when EndReason == KickoffEndReason.OutOfBounds =>
            "OUT OF BOUNDS  |  Press K to kick off again",
        KickoffPhase.Dead when EndReason == KickoffEndReason.EndZone =>
            "END ZONE  |  Press K to kick off again",
        KickoffPhase.Dead when EndReason == KickoffEndReason.Tackled =>
            "TACKLED  |  Press K to kick off again",
        _ => "PLAY OVER  |  Press K to kick off again"
    };

    public void Begin(Vector2 kicker, Vector2 returner, PlayerAttributes? attributes = null)
    {
        _origin = kicker;
        _landing = returner;
        _returnerAttributes = attributes ?? PlayerAttributes.KickoffReturnTeam(0);
        _possession.BeginFlight(kicker, BallPlayType.Kickoff);
        BallPosition = kicker;
        BallFrame = 8;
        LastCatchResult = null;
        EndReason = KickoffEndReason.None;
        _elapsed = 0f;
        _looseTime = 0f;
        _bounceStartFrame = 16;
        Phase = KickoffPhase.Windup;
    }

    public void Update(float deltaTime, Vector2 returner, CatchFactors factors = default)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        while (deltaTime > 0f)
        {
            float step = Math.Min(deltaTime, 0.05f);
            Step(step, returner, factors);
            deltaTime -= step;
        }
    }

    private void Step(float deltaTime, Vector2 returner, CatchFactors factors)
    {
        switch (Phase)
        {
            case KickoffPhase.Windup:
                _elapsed += deltaTime;
                BallPosition = _origin;
                if (_elapsed >= WindupSeconds)
                {
                    Phase = KickoffPhase.Flight;
                    _elapsed = 0f;
                }
                break;
            case KickoffPhase.Flight:
                _elapsed = Math.Min(FlightSeconds, _elapsed + deltaTime);
                float progress = _elapsed / FlightSeconds;
                Vector2 ground = Vector2.Lerp(_origin, _landing, progress);
                BallPosition = ground - new Vector2(0f,
                    MathF.Sin(progress * MathF.PI) * ActionProfiles.Weights.Kickoff.ArcHeight);
                _possession.SetFlightPosition(BallPosition);
                BallFrame = 8 + ((int)(_elapsed /
                    ActionProfiles.Weights.Kickoff.SpiralFrameSeconds) % 8);
                if (_elapsed >= FlightSeconds)
                {
                    BallPosition = _landing;
                    _possession.SetFlightPosition(_landing);
                    _elapsed = 0f;
                    if (Near(returner, _landing, CatchRadius))
                        AttemptCatch(returner, factors);
                    else
                    {
                        _possession.LandAt(_landing);
                        StartBounce(returner);
                    }
                }
                break;
            case KickoffPhase.Bounce:
                _possession.Update(deltaTime, null, _landing.Y);
                BallPosition = _possession.Position;
                _elapsed += deltaTime;
                BallFrame = _bounceStartFrame + BounceFrame(_elapsed);
                if (_possession.CanAttemptCatch && Near(returner, BallPosition, CatchRadius))
                    AttemptCatch(returner, factors);
                else if (_possession.State == BallPossessionState.LooseBall)
                    Phase = KickoffPhase.Loose;
                else if (_possession.State == BallPossessionState.DeadBall)
                    Phase = KickoffPhase.Dead;
                break;
            case KickoffPhase.Loose:
                _looseTime += deltaTime;
                if (_possession.CanAttemptCatch && Near(returner, BallPosition, CatchRadius))
                    AttemptCatch(returner, factors);
                else if (_looseTime >= ActionProfiles.Weights.Kickoff.LooseBallTimeoutSeconds)
                {
                    _possession.MarkDead(BallDeadReason.EndOfPlay);
                    Phase = KickoffPhase.Dead;
                }
                break;
            case KickoffPhase.Return:
                _possession.Update(deltaTime, returner, _landing.Y);
                BallPosition = _possession.Position;
                BallFrame = 0;
                break;
        }
    }

    private void AttemptCatch(Vector2 returner, CatchFactors factors)
    {
        LastCatchResult = _catchHandler.Attempt(_possession, _returnerAttributes,
            factors, playerId: 0, position: returner);
        if (_possession.IsCarried)
        {
            Phase = KickoffPhase.Return;
            BallPosition = _possession.Position;
            BallFrame = 0;
        }
        else
            StartBounce(returner);
    }

    private void StartBounce(Vector2 returner)
    {
        float missX = MathF.Abs(returner.X - _landing.X);
        float missY = MathF.Abs(returner.Y - _landing.Y);
        _bounceStartFrame = missX >= 12f ? 32 : missY >= 12f ? 24 : 16;
        _elapsed = 0f;
        BallFrame = _bounceStartFrame;
        Phase = KickoffPhase.Bounce;
    }

    public void FumbleAt(Vector2 position)
    {
        if (Phase != KickoffPhase.Return)
            throw new InvalidOperationException("Only a carrier can fumble.");
        _possession.LosePossession(position);
        BallPosition = position;
        _looseTime = 0f;
        Phase = KickoffPhase.Loose;
    }

    public void EndReturn(KickoffEndReason reason = KickoffEndReason.Tackled)
    {
        if (reason == KickoffEndReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));
        if (Phase is not (KickoffPhase.Return or KickoffPhase.Loose)) return;
        EndReason = reason;
        _possession.MarkDead(BallDeadReason.EndOfPlay);
        Phase = KickoffPhase.Dead;
    }

    private static int BounceFrame(float elapsed)
    {
        float total = 0f;
        for (int index = 0; index < BounceFrameSeconds.Length; index++)
        {
            total += BounceFrameSeconds[index];
            if (elapsed < total) return index;
        }
        return BounceFrameSeconds.Length - 1;
    }

    internal static bool Near(Vector2 a, Vector2 b, float radius) =>
        Vector2.DistanceSquared(a, b) <= radius * radius;
}
