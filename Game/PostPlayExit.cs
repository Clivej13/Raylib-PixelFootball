using System.Numerics;

namespace RaylibPixelFootball.Game;

// The next-play lists are empty for now. This keeps the selection rule ready
// for substitutions while walking every current player to their own sideline.
internal sealed class PostPlayExit
{
    private readonly List<(PlayerEntity Player, Vector2 Destination)> _departing = [];
    private readonly HashSet<PlayerEntity> _exited = [];

    public bool Active { get; private set; }
    public int RemainingCount => _departing.Count - _exited.Count;
    public bool HasExited(PlayerEntity player) => _exited.Contains(player);

    public void Begin(IReadOnlyList<PlayerEntity> current,
        IReadOnlyCollection<PlayerEntity> nextHome,
        IReadOnlyCollection<PlayerEntity> nextAway,
        float fieldTop, float fieldBottom)
    {
        Reset();
        Active = true;
        for (int index = 0; index < current.Count; index++)
        {
            PlayerEntity player = current[index];
            bool stays = player.Team == TeamId.Home
                ? nextHome.Contains(player) : nextAway.Contains(player);
            if (stays) continue;
            float x = player.Team == TeamId.Home ? 24f : 296f;
            float y = Math.Clamp(player.Position.Y + (index % 7 - 3) * 2.5f,
                fieldTop + PlayerEntity.DisplaySize,
                fieldBottom - PlayerEntity.DisplaySize);
            _departing.Add((player, new Vector2(x, y)));
        }
    }

    public void Update(float deltaTime, float fieldTop, float fieldBottom,
        IReadOnlyList<PlayerEntity> fieldPlayers)
    {
        if (!Active) return;
        PlayerEntity[] activePlayers = fieldPlayers
            .Where(player => !_exited.Contains(player)).ToArray();
        foreach (var (player, destination) in _departing)
        {
            if (_exited.Contains(player)) continue;
            player.WalkToSideline(destination, deltaTime, fieldTop, fieldBottom,
                activePlayers);
            if ((player.Team == TeamId.Home && player.Position.X <= 32f)
                || (player.Team == TeamId.Away && player.Position.X >= 288f))
                _exited.Add(player);
        }
    }

    public void Reset()
    {
        Active = false;
        _departing.Clear();
        _exited.Clear();
    }
}
