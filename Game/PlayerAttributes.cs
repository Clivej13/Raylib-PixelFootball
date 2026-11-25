namespace RaylibPixelFootball.Game;

internal enum PlayerAttribute
{
    Blocking, BlockShedding, Strength, Catching, Tackling, BreakTackle,
    BallSecurity, StripBall, ThrowAccuracy, Agility, Coverage, RouteRunning,
    Press, Release, ContestedCatching, PassDefence
}

internal sealed class PlayerAttributes
{
    private readonly IReadOnlyDictionary<PlayerAttribute, int> _ratings;

    public PlayerAttributes(IReadOnlyDictionary<PlayerAttribute, int>? ratings = null)
    {
        var values = Enum.GetValues<PlayerAttribute>()
            .ToDictionary(attribute => attribute,
                _ => ActionProfiles.Weights.PlayerRatings.DefaultRating);
        if (ratings is not null)
            foreach (var (attribute, rating) in ratings)
            {
                if (!Enum.IsDefined(attribute)) throw new ArgumentOutOfRangeException(nameof(ratings));
                if (rating is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(ratings));
                values[attribute] = rating;
            }
        _ratings = values;
    }

    public int Get(PlayerAttribute attribute) => _ratings[attribute];

    // Tackling and BreakTackle use the configurable default until player ratings are added.
    public static PlayerAttributes KickoffCoverage(int index)
    {
        PlayerRatingWeights ratings = ActionProfiles.Weights.PlayerRatings;
        return new PlayerAttributes(new Dictionary<PlayerAttribute, int>
        {
            [PlayerAttribute.BlockShedding] = ratings.CoverageBlockShedding.At(index),
            [PlayerAttribute.Strength] = ratings.CoverageStrength.At(index),
            [PlayerAttribute.StripBall] = ratings.CoverageStripBall.At(index)
        });
    }

    public static PlayerAttributes KickoffReturnTeam(int index)
    {
        PlayerRatingWeights ratings = ActionProfiles.Weights.PlayerRatings;
        return new PlayerAttributes(new Dictionary<PlayerAttribute, int>
        {
            [PlayerAttribute.Blocking] = ratings.ReturnBlocking.At(index),
            [PlayerAttribute.Strength] = ratings.ReturnStrength.At(index),
            [PlayerAttribute.Catching] = ratings.ReturnCatching.At(index),
            [PlayerAttribute.BallSecurity] = ratings.ReturnBallSecurity.At(index)
        });
    }
}
