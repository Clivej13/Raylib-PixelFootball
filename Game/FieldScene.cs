using System.Numerics;
using RaylibGameFramework.Input;

namespace RaylibPixelFootball.Game;

// A playable kickoff on the existing 22-player field and 75-player sideline scene.
internal sealed class FieldScene
{
    private PlayerEntity[] _fieldPlayers = [];
    private PlayerEntity[] _sidelinePlayers = [];
    private PlayerEntity? _runner;
    private PlayerEntity? _kicker;
    private StadiumArtwork? _artwork;
    private PlayerSpriteAssets? _sprites;
    private BallSpriteAssets? _ballSprites;
    private readonly ActionResolver _resolver = new();
    private readonly KickoffPlay _kickoff;
    private readonly TackleHandler _tackleHandler;
    private readonly BallSecurityHandler _securityHandler;
    private TackleStruggle? _tackleStruggle;
    private PlayerEntity? _struggleTackler;
    private PlayerEntity? _contactTackler;
    private readonly TackleContactCue _contactCue = new();
    private readonly TackleContactGate _tackleContacts = new();
    private readonly PileRecoveryQueue _recoveryQueue = new();
    private readonly PostPlayExit _postPlayExit = new();
    private int _observedCatchAttempts;
    private KickoffBlocking? _blocks;
    private readonly FieldScroller _scroller;

    public FieldScene(float scrollEdgeBuffer)
    {
        _scroller = new FieldScroller(180f, scrollEdgeBuffer, PlayerEntity.DisplaySize);
        _kickoff = new KickoffPlay(_resolver);
        _tackleHandler = new TackleHandler(_resolver);
        _securityHandler = new BallSecurityHandler(_resolver);
    }

    public int FieldPlayerCount => _fieldPlayers.Length;
    public int SidelinePlayerCount => _sidelinePlayers.Length;
    public Vector2 PlayerPosition => _runner?.Position ?? new Vector2(160, -82);
    public string PlayerTeam => _runner?.Team.ToString() ?? "Away";
    public string PlayerAnimation => _runner?.AnimationName ?? "Idle";
    public int PlayerDirection => _runner?.DirectionIndex ?? 4;
    public int PlayerFrame => _runner?.FrameIndex ?? 0;
    public float CameraTop => _scroller.Top;
    public string KickoffStatus => _kickoff.Status;
    public string KickoffPhaseName => _kickoff.Phase.ToString();
    public int ActiveBlockCount => _blocks?.ActiveCount ?? 0;
    public string BallStateName => _kickoff.Ball.State.ToString();
    public float ReturnerRecovery => _runner?.RecoveryRemaining ?? 0f;
    public ActionResult? LastBlockResult => _blocks?.LastResolution;
    public ActionResult? LastCatchResult => _kickoff.LastCatchResult;
    public ActionResult? LastTackleResult { get; private set; }
    public ActionResult? LastSecurityResult { get; private set; }
    public ActionResult? LatestActionResult { get; private set; }
    public string LatestActionName { get; private set; } = "None";
    public float LastEngagementSeconds => _blocks?.LastEngagementSeconds ?? 0f;
    public float LastBlockDisplacementYards => _blocks?.LastDisplacementYards ?? 0f;

    public void AttachArtwork(StadiumArtwork artwork)
    {
        _artwork = artwork;
        _scroller.SetFieldBounds(artwork.GameplayFieldTop, artwork.GameplayFieldBottom);
    }

    public void AttachSprites(PlayerSpriteAssets assets)
    {
        _sprites = assets;
        _fieldPlayers = FieldPopulation.CreateField()
            .Select((spawn, index) => new PlayerEntity(assets, spawn,
                index == 16 ? PlayerAnimationKind.Idle : PlayerAnimationKind.Run,
                index < 11 ? PlayerAttributes.KickoffCoverage(index)
                    : PlayerAttributes.KickoffReturnTeam(index - 11)))
            .ToArray();
        _sidelinePlayers = FieldPopulation.CreateSidelines()
            .Select(spawn => new PlayerEntity(assets, spawn))
            .ToArray();
        _kicker = _fieldPlayers[5];
        _runner = _fieldPlayers[16];
        _blocks = new KickoffBlocking(_fieldPlayers.Take(11).ToArray(),
            _fieldPlayers.Skip(11).Where(player => !ReferenceEquals(player, _runner)).ToArray(),
            _resolver);
    }

    public void AttachBall(BallSpriteAssets ballSprites) => _ballSprites = ballSprites;

    public void SetTeamColors(string homeColor, string awayColor) =>
        _sprites?.SetColors(homeColor, awayColor);

    public void Reset()
    {
        if (_runner is null || _kicker is null) return;
        for (int index = 0; index < _fieldPlayers.Length; index++)
        {
            int column = index % 11;
            bool coverage = index < 11;
            float y = coverage ? 145f : -28f;
            if (index == 5) y = 150f;
            if (index == 16) y = -82f;
            _fieldPlayers[index].Reset(new Vector2(65f + column * 19f, y),
                coverage ? 0 : 4);
        }
        _blocks?.Reset();
        _observedCatchAttempts = 0;
        _tackleContacts.Reset();
        _recoveryQueue.Reset();
        _postPlayExit.Reset();
        _tackleStruggle = null;
        _struggleTackler = null;
        _contactTackler = null;
        _contactCue.Reset();
        LastTackleResult = null;
        LastSecurityResult = null;
        LatestActionResult = null;
        LatestActionName = "None";
        _kickoff.Begin(_kicker.Position, _runner.Position, _runner.Attributes);
        _kicker.StartAction(PlayerAnimationKind.Kick);
        _scroller.Reset();
        _scroller.Follow(_kickoff.CameraTarget.Y);
    }

    public void Update(InputController input, float deltaTime)
    {
        if (_runner is null || _kicker is null) return;
        if (_kickoff.Phase == KickoffPhase.Dead)
        {
            if (input.WasPressed("Kick")) Reset();
            else
            {
                if (!_postPlayExit.Active)
                    _postPlayExit.Begin(_fieldPlayers, [], [], FieldTop, FieldBottom);
                _postPlayExit.Update(deltaTime, FieldTop, FieldBottom, _fieldPlayers);
                if (_contactTackler is { } tackler
                    && _contactCue.Consume(_runner.Animation, _runner.FrameIndex))
                    CompleteTackleAtContact(tackler);
                foreach (PlayerEntity player in _sidelinePlayers)
                    player.AnimateInPlace(deltaTime);
            }
            return;
        }

        float x = MathF.Abs(input.GetValue("MoveRight")) - MathF.Abs(input.GetValue("MoveLeft"));
        float y = MathF.Abs(input.GetValue("MoveDown")) - MathF.Abs(input.GetValue("MoveUp"));
        if (_kickoff.Phase == KickoffPhase.Return)
            HandleActionInput(input);
        _runner.Update(new Vector2(x, y), deltaTime, FieldTop, FieldBottom, _fieldPlayers);
        if (_contactTackler is { } contactTackler
            && _contactCue.Consume(_runner.Animation, _runner.FrameIndex))
            CompleteTackleAtContact(contactTackler);

        SituationalWeights situation = ActionProfiles.Weights.Situational;
        bool contested = _fieldPlayers.Take(11).Any(player =>
            KickoffPlay.Near(player.Position, _runner.Position,
                situation.CatchCoverageRadius));
        var catchFactors = new CatchFactors(BallVelocity: situation.CatchBallVelocity,
            DefensiveCoverage: contested ? situation.CatchDefensiveCoverage : 0,
            DefensiveContact: _fieldPlayers.Take(11).Any(player =>
                KickoffPlay.Near(player.Position, _runner.Position,
                    situation.CatchContactRadius))
                ? situation.CatchDefensiveContact : 0,
            ReceiverMovement: x * x + y * y
                > situation.CatchMovementThresholdSquared
                ? situation.CatchReceiverMovement : 0,
            BodyPosition: _runner.DirectionIndex == 4 ? situation.CatchBodyPosition : 0,
            CatchRadius: Vector2.DistanceSquared(_runner.Position, _kickoff.LandingPosition)
                > situation.CatchRadiusPenaltyDistanceSquared
                ? situation.CatchRadius : 0);
        _kickoff.Update(deltaTime, _runner.Position, catchFactors);
        if (_kickoff.Ball.CatchAttempts > _observedCatchAttempts
            && _kickoff.LastCatchResult is { } catchResult)
        {
            _observedCatchAttempts = _kickoff.Ball.CatchAttempts;
            LatestActionResult = catchResult;
            LatestActionName = "Catch";
            _runner.ApplyCatchControl(ActionProfiles.Catching.ConsequenceFor(catchResult.Outcome));
            PlayerAnimationKind animation = catchResult.Outcome switch
            {
                ActionOutcome.CriticalFailure => PlayerAnimationKind.CatchDive,
                ActionOutcome.MinorSuccess or ActionOutcome.MajorSuccess => PlayerAnimationKind.CatchRac,
                ActionOutcome.CriticalSuccess => PlayerAnimationKind.CatchHigh,
                _ => PlayerAnimationKind.CatchStationary
            };
            _runner.StartAction(animation, interrupt: true);
        }

        _runner.SetBallCarrier(_kickoff.Ball.IsCarried);
        if (_kickoff.Phase == KickoffPhase.Return)
        {
            KickoffEndReason boundary = FieldPlayBounds.CheckCarrier(
                _runner.Position, FieldTop, FieldBottom);
            if (boundary != KickoffEndReason.None)
                _kickoff.EndReturn(boundary);
        }
        if (_kickoff.Phase != KickoffPhase.Dead)
            UpdateAi(deltaTime);
        if (_kickoff.Phase == KickoffPhase.Return)
            ResolveTackle(deltaTime);
        _runner.SetBallCarrier(_kickoff.Ball.IsCarried);
        if (_kickoff.Phase == KickoffPhase.Dead)
        {
            _blocks?.EndPlay();
            _struggleTackler?.EndEngage();
            _tackleStruggle = null;
            _struggleTackler = null;
        }
        _scroller.Follow(_kickoff.CameraTarget.Y);
        foreach (PlayerEntity player in _sidelinePlayers)
            player.AnimateInPlace(deltaTime);
    }

    private float FieldTop => _artwork?.GameplayFieldTop ?? -170f;
    private float FieldBottom => _artwork?.GameplayFieldBottom ?? 350f;

    private void ResolveTackle(float deltaTime)
    {
        if (_runner is null || _contactTackler is not null) return;
        _tackleContacts.Advance(deltaTime);
        if (_tackleStruggle is not null)
        {
            TackleStruggleOutcome outcome = _tackleStruggle.Update(deltaTime);
            if (_tackleStruggle.LastResult is { } check && !ReferenceEquals(LastTackleResult, check.Action))
            {
                LastTackleResult = check.Action;
                LatestActionResult = check.Action;
                LatestActionName = "Tackle struggle";
            }
            if (outcome == TackleStruggleOutcome.Active) return;
            _struggleTackler?.EndEngage();
            if (outcome == TackleStruggleOutcome.Tackled && _struggleTackler is not null)
                BeginCompletedTackle(_struggleTackler,
                    _tackleStruggle.LastResult?.Effect == TackleEffect.Dominant);
            else
            {
                if (_struggleTackler is not null)
                    _tackleContacts.Mark(_struggleTackler);
                _struggleTackler?.StartAction(PlayerAnimationKind.BlockLoss, interrupt: true);
                TackleEffectWeights escape = ActionProfiles.Weights.TackleEffects;
                _runner.ApplyCatchControl(new ActionConsequence(
                    RecoverySeconds: escape.EscapeRecoverySeconds,
                    MomentumRetention: escape.EscapeMomentumRetention));
                _runner.StartAction(BreakTackleAnimations.ForEscapedWrap(),
                    interrupt: true);
            }
            _tackleStruggle = null;
            _struggleTackler = null;
            return;
        }

        PlayerEntity? tackler = _blocks?.TakeTacklerAt(
            _runner.Position, _tackleContacts.Excluded);
        if (tackler is null) return;
        _tackleContacts.Mark(tackler);
        TackleResult tackle = _tackleHandler.Resolve(tackler.Attributes, _runner.Attributes,
            [new ActionModifier("Unsecured ball",
                _kickoff.Ball.State == BallPossessionState.UnsecuredPossession
                    ? ActionProfiles.Weights.Situational.UnsecuredBall : 0)]);
        LastTackleResult = tackle.Action;
        LatestActionResult = tackle.Action;
        LatestActionName = "Tackle";
        if (BreakTackleAnimations.ForContact(tackle.Effect) is { } carrierAnimation)
            _runner.StartAction(carrierAnimation, interrupt: true);
        switch (tackle.Effect)
        {
            case TackleEffect.Miss:
            case TackleEffect.Broken:
                tackler.KnockDown(DownEvent.MissedTackle,
                    ActionProfiles.Weights.TackleEffects.MissedTackleDownSeconds,
                    PlayerAnimationKind.BlockLoss);
                break;
            case TackleEffect.Glance:
                TackleEffectWeights glance = ActionProfiles.Weights.TackleEffects;
                _runner.ApplyCatchControl(new ActionConsequence(
                    RecoverySeconds: glance.GlanceRecoverySeconds,
                    MomentumRetention: glance.GlanceMomentumRetention));
                break;
            case TackleEffect.Wrap:
            case TackleEffect.Controlled:
                _tackleStruggle = new TackleStruggle(_tackleHandler,
                    tackler.Attributes, _runner.Attributes,
                    tackle.Effect == TackleEffect.Controlled ? 1 : 0);
                _struggleTackler = tackler;
                tackler.BeginEngage();
                TackleEffectWeights wrap = ActionProfiles.Weights.TackleEffects;
                _runner.ApplyCatchControl(new ActionConsequence(
                    RecoverySeconds: wrap.WrapRecoverySeconds,
                    MomentumRetention: wrap.WrapMomentumRetention));
                break;
            case TackleEffect.Complete:
                BeginCompletedTackle(tackler, dominant: false);
                break;
            case TackleEffect.Dominant:
                BeginCompletedTackle(tackler, dominant: true);
                break;
        }
    }

    private void BeginCompletedTackle(PlayerEntity tackler, bool dominant)
    {
        if (_runner is null) return;
        _contactTackler = tackler;
        _contactCue.Arm();
        tackler.BeginEngage();
        TackleEffectWeights effects = ActionProfiles.Weights.TackleEffects;
        _runner.KnockDown(DownEvent.Tackled, dominant
            ? effects.DominantTackleDownSeconds : effects.CompletedTackleDownSeconds);
        if (dominant)
        {
            LastSecurityResult = _securityHandler.Resolve(_runner.Attributes, tackler.Attributes);
            LatestActionResult = LastSecurityResult;
            LatestActionName = "Ball security";
            if (BallSecurityHandler.IsFumble(LastSecurityResult))
                _kickoff.FumbleAt(_runner.Position);
        }
        _kickoff.EndReturn(KickoffEndReason.Tackled);
    }

    private void CompleteTackleAtContact(PlayerEntity tackler)
    {
        _contactTackler = null;
        if (_runner is null) return;
        tackler.MoveEngaged(_runner.Position - tackler.Position, FieldTop, FieldBottom);
        var pile = _fieldPlayers.Where(player => player.IsDowned
            && !ReferenceEquals(player, _runner)
            && Vector2.DistanceSquared(player.Position, _runner.Position)
                <= PlayerCollision.MinimumSeparation * PlayerCollision.MinimumSeparation)
            .ToList();
        pile.Add(_runner);
        pile.Add(tackler);
        _recoveryQueue.Register(pile);
        foreach (PlayerEntity player in pile)
            player.JoinRecoveryQueue(_recoveryQueue);
        tackler.BeginTackleRecovery();
    }

    private void HandleActionInput(InputController input)
    {
        if (_runner is null) return;
        if (input.WasPressed("Throw")) _runner.StartAction(PlayerAnimationKind.Throw);
        else if (input.WasPressed("CatchStationary")) _runner.StartAction(PlayerAnimationKind.CatchStationary);
        else if (input.WasPressed("CatchRac")) _runner.StartAction(PlayerAnimationKind.CatchRac);
        else if (input.WasPressed("CatchHigh")) _runner.StartAction(PlayerAnimationKind.CatchHigh);
        else if (input.WasPressed("CatchDive")) _runner.StartAction(PlayerAnimationKind.CatchDive);
        else if (input.WasPressed("Kick")) _runner.StartAction(PlayerAnimationKind.Kick);
        else if (input.WasPressed("BullRush")) _runner.StartAction(PlayerAnimationKind.BullRush);
        else if (input.WasPressed("Engage")) _runner.StartAction(PlayerAnimationKind.Engage);
        else if (input.WasPressed("BlockLoss")) _runner.StartAction(PlayerAnimationKind.BlockLoss);
    }

    private void UpdateAi(float deltaTime)
    {
        if (_runner is null) return;
        if (_kickoff.Phase == KickoffPhase.Windup)
        {
            foreach (PlayerEntity player in _fieldPlayers)
                if (!ReferenceEquals(player, _runner))
                    player.AnimateInPlace(deltaTime);
            return;
        }

        Vector2 pursuitTarget = _kickoff.Phase == KickoffPhase.Return
            ? _runner.Position : _kickoff.LandingPosition;
        ActionResult? previousBlock = _blocks?.LastResolution;
        _blocks?.Update(deltaTime, pursuitTarget, FieldTop, FieldBottom, _fieldPlayers);
        if (_blocks?.LastResolution is { } block && !ReferenceEquals(block, previousBlock))
        {
            LatestActionResult = block;
            LatestActionName = "Block";
        }
    }

    public void Draw()
    {
        if (_artwork is null)
            throw new InvalidOperationException("Field artwork must be attached before drawing.");
        _artwork.DrawGameplayField();
        foreach (PlayerEntity player in _sidelinePlayers)
            player.Draw();
        foreach (PlayerEntity player in _fieldPlayers
            .Where(player => !_postPlayExit.HasExited(player))
            .OrderBy(player => player.IsDowned ? 0 : 1)
            .ThenBy(player => player.Position.Y)
            .ThenBy(player => _recoveryQueue.Depth(player)))
            player.Draw();
        if (!_kickoff.Ball.IsCarried
            && _kickoff.Ball.State != BallPossessionState.DeadBall)
            _ballSprites?.Draw(_kickoff.BallPosition, _kickoff.BallFrame);
    }
}
