namespace SnakesLadders.Game;

/// <summary>A times-table fact such as 7 × 8. The order the numbers are shown in is random.</summary>
public readonly record struct Fact(int Left, int Right)
{
    public int Product => Left * Right;

    /// <summary>7 × 8 and 8 × 7 count as the same fact.</summary>
    public (int Low, int High) Key => (Math.Min(Left, Right), Math.Max(Left, Right));

    public override string ToString() => $"{Left} × {Right}";
}

/// <summary>
/// The fact written in each of the 100 squares. There are more squares than facts, so each new fact
/// is one of the least used so far, which spreads them evenly. Squares a single roll
/// apart (up to 6) never share a fact, and a square that gets a new fact never gets the one it had.
/// </summary>
public sealed class FactSheet
{
    public const int LowestFactor = 2;

    /// <summary>Squares this close together never show the same fact, so one roll never offers a repeat.</summary>
    public const int NearbyDistance = 6;

    private readonly Random _random;
    private readonly Dictionary<int, Fact> _facts = new();

    public FactSheet(Random random)
    {
        _random = random;
    }

    /// <summary>The biggest number in a fact, for example 12 for facts up to 12 × 12.</summary>
    public int HighestFactor { get; private set; } = 12;

    public Fact this[int square] => _facts[square];

    public IReadOnlyDictionary<int, Fact> All => _facts;

    /// <summary>Gives every square a fresh fact, for a new game.</summary>
    public void DealAll(IEnumerable<int> squares, int highestFactor)
    {
        HighestFactor = Math.Max(LowestFactor + 1, highestFactor);
        _facts.Clear();
        foreach (var square in squares)
        {
            _facts[square] = Draw(square, previous: null);
        }
    }

    /// <summary>Gives one square a new fact, different from the one it had.</summary>
    public void Replace(int square)
    {
        var previous = _facts[square];
        _facts.Remove(square);
        _facts[square] = Draw(square, previous);
    }

    private Fact Draw(int square, Fact? previous)
    {
        var nearby = _facts
            .Where(pair => Math.Abs(pair.Key - square) <= NearbyDistance)
            .Select(pair => pair.Value.Key)
            .ToHashSet();
        var used = _facts.Values.GroupBy(fact => fact.Key).ToDictionary(group => group.Key, group => group.Count());

        // Facts that aren't next door and aren't the one this square just had. With very few facts to choose
        // from (facts up to 5) that can rule out everything, so then only the square's old fact is ruled out.
        var choices = AllKeys().Where(key => previous?.Key != key && !nearby.Contains(key)).ToList();
        if (choices.Count == 0)
        {
            choices = AllKeys().Where(key => previous?.Key != key).ToList();
        }

        // The facts used least so far, so every fact turns up about as often as the others.
        var fewest = choices.Min(key => used.GetValueOrDefault(key));
        var pool = choices.Where(key => used.GetValueOrDefault(key) == fewest).ToList();
        var (a, b) = pool[_random.Next(pool.Count)];
        return _random.Next(2) == 0 ? new Fact(a, b) : new Fact(b, a);
    }

    private IEnumerable<(int Low, int High)> AllKeys()
    {
        for (var low = LowestFactor; low <= HighestFactor; low++)
        {
            for (var high = low; high <= HighestFactor; high++)
            {
                yield return (low, high);
            }
        }
    }
}
