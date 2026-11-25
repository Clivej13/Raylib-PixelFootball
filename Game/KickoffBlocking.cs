using System.Numerics;

namespace RaylibPixelFootball.Game;

// Assigns nearby opponents as play develops; contests use the shared action resolver.
internal sealed class KickoffBlocking
{
    private readonly CoverageAgent[] _coverage;
    private readonly BlockerAgent[] _blockers;
    private readonly List<LiveBlock> _active = [];
    private readonly HashSet<(CoverageAgent Coverage, BlockerAgent Blocker)> _finishedPairs = [];
    private readonly ActionResolver _resolver;
    private readonly ActionConfig _profile;

    private sealed record LiveBlock(CoverageAgent Coverage, BlockerAgent Blocker,
        BlockContest Contest);

    public int ActiveCount => _active.Count;
    public ActionResult? LastResolution { get; private set; }
    public float LastEngagementSeconds { get; private set; }
    public float LastDisplacementYards { get; private set; }

    public KickoffBlocking(IReadOnlyList<PlayerEntity> coverage, IReadOnlyList<PlayerEntity> blockers,
        ActionResolver? resolver = null, ActionConfig? profile = null)
    {
        if (coverage.Count == 0 || blockers.Count == 0)
            throw new ArgumentException("Kickoff blocking needs both teams.");
        _resolver = resolver ?? new ActionResolver();
        _profile = profile ?? ActionProfiles.Blocking;
        _blockers = blockers.Select(player => new BlockerAgent(player)).ToArray();
        _coverage = coverage.Select(player => new CoverageAgent(player, blockers)).ToArray();
    }

    public void Reset()
    {
        EndPlay();
        foreach (CoverageAgent agent in _coverage) agent.Reset();
        foreach (BlockerAgent agent in _blockers) agent.Reset();
        LastResolution = null;
        LastEngagementSeconds = 0f;
        LastDisplacementYards = 0f;
    }

    public void EndPlay()
    {
        foreach (LiveBlock live in _active)
        {
            live.Coverage.ReleaseEngagement();
            live.Blocker.ReleaseEngagement();
        }
        _active.Clear();
        _finishedPairs.Clear();
    }

    public PlayerEntity? GetTacklerAt(Vector2 carrier,
        IReadOnlyCollection<PlayerEntity>? excluded = null) =>
        _coverage.Where(agent => agent.CanTackle
                && (excluded is null || !excluded.Contains(agent.Player))
                && KickoffPlay.Near(agent.Position, carrier, BlockContest.TackleRadius))
            .OrderBy(agent => Vector2.DistanceSquared(agent.Position, carrier))
            .Select(agent => agent.Player).FirstOrDefault();

    public PlayerEntity? TakeTacklerAt(Vector2 carrier,
        IReadOnlyCollection<PlayerEntity>? excluded = null)
    {
        PlayerEntity? free = GetTacklerAt(carrier, excluded);
        if (free is not null) return free;

        // Contact with an engaged defender interrupts that block and starts a tackle.
        LiveBlock? contact = _active
            .Where(live => (excluded is null || !excluded.Contains(live.Coverage.Player))
                && KickoffPlay.Near(live.Coverage.Position, carrier,
                    BlockContest.TackleRadius))
            .OrderBy(live => Vector2.DistanceSquared(live.Coverage.Position, carrier))
            .FirstOrDefault();
        if (contact is null) return null;
        contact.Coverage.ReleaseEngagement();
        contact.Blocker.ReleaseEngagement();
        _finishedPairs.Add((contact.Coverage, contact.Blocker));
        _active.Remove(contact);
        return contact.Coverage.Player;
    }

    public bool HasTacklerAt(Vector2 carrier) => GetTacklerAt(carrier) is not null;

    public void Update(float deltaTime, Vector2 carrier, float fieldTop, float fieldBottom,
        IReadOnlyList<PlayerEntity> fieldPlayers)
    {
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        while (deltaTime > 0f)
        {
            float step = Math.Min(deltaTime, 0.05f);
            Step(step, carrier, fieldTop, fieldBottom, fieldPlayers);
            deltaTime -= step;
        }
    }

    private void Step(float deltaTime, Vector2 carrier, float fieldTop, float fieldBottom,
        IReadOnlyList<PlayerEntity> fieldPlayers)
    {
        foreach (CoverageAgent agent in _coverage)
            agent.Update(deltaTime, carrier, null, fieldTop, fieldBottom, fieldPlayers);

        var reserved = new HashSet<CoverageAgent>();
        foreach (BlockerAgent blocker in _blockers
            .OrderBy(agent => Vector2.DistanceSquared(agent.Position, carrier)))
        {
            CoverageAgent? threat = _coverage
                .Where(agent => agent.CanEngage && !reserved.Contains(agent)
                    && !_finishedPairs.Contains((agent, blocker)))
                .OrderBy(agent => Vector2.DistanceSquared(blocker.Position, agent.Position))
                .FirstOrDefault();
            if (threat is not null) reserved.Add(threat);
            blocker.Update(deltaTime, carrier, threat, fieldTop, fieldBottom, fieldPlayers);
        }

        for (int index = _active.Count - 1; index >= 0; index--)
        {
            LiveBlock live = _active[index];
            BlockOutcome outcome = live.Contest.Update(deltaTime,
                live.Coverage.Position, live.Blocker.Position, carrier);
            if (live.Contest.MovementDelta != Vector2.Zero)
            {
                Vector2[] others = fieldPlayers
                    .Where(player => player.BlocksMovement
                        && !ReferenceEquals(player, live.Coverage.Player)
                        && !ReferenceEquals(player, live.Blocker.Player))
                    .Select(player => player.Position).ToArray();
                Vector2 shift = EngagementMotion.AllowedShift(live.Contest.MovementDelta,
                    live.Coverage.Position, live.Blocker.Position, fieldTop, fieldBottom, others);
                live.Coverage.Player.MoveEngaged(shift, fieldTop, fieldBottom);
                live.Blocker.Player.MoveEngaged(shift, fieldTop, fieldBottom);
            }
            if (live.Contest.LastResult is { } result && !ReferenceEquals(LastResolution, result))
            {
                LastResolution = result;
                LastEngagementSeconds = live.Contest.Elapsed;
                LastDisplacementYards = live.Contest.TotalDisplacementYards;
            }
            if (outcome == BlockOutcome.Active) continue;
            _finishedPairs.Add((live.Coverage, live.Blocker));
            if (outcome == BlockOutcome.Released)
            {
                live.Coverage.ReleaseEngagement();
                live.Blocker.ReleaseEngagement();
            }
            else
            {
                float recovery = _profile.ConsequenceFor(
                    outcome == BlockOutcome.CoverageWins
                        ? ActionOutcome.CriticalFailure : ActionOutcome.CriticalSuccess).RecoverySeconds;
                live.Coverage.FinishEngagement(outcome == BlockOutcome.CoverageWins, recovery);
                live.Blocker.FinishEngagement(outcome == BlockOutcome.BlockerWins, recovery);
            }
            _active.RemoveAt(index);
        }

        foreach (BlockerAgent blocker in _blockers)
        {
            if (!blocker.CanEngage) continue;
            CoverageAgent? coverage = _coverage
                .Where(agent => agent.CanEngage
                    && !_finishedPairs.Contains((agent, blocker))
                    && KickoffPlay.Near(agent.Position, blocker.Position,
                        BlockContest.ContactRadius))
                .OrderBy(agent => Vector2.DistanceSquared(agent.Position, blocker.Position))
                .FirstOrDefault();
            if (coverage is null) continue;
            coverage.BeginEngagement();
            blocker.BeginEngagement();
            _active.Add(new LiveBlock(coverage, blocker,
                new BlockContest(
                    blocker.Player.Attributes.Get(PlayerAttribute.Blocking),
                    coverage.Player.Attributes.Get(PlayerAttribute.BlockShedding),
                    blocker.Player.Attributes.Get(PlayerAttribute.Strength),
                    coverage.Player.Attributes.Get(PlayerAttribute.Strength),
                    _resolver, _profile)));
        }
    }
}
