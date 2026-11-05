namespace TicTacToe.Game;

/// <summary>A times-table fact such as 7 × 8. The order the numbers are shown in is random.</summary>
public readonly record struct Fact(int Left, int Right)
{
    public int Product => Left * Right;

    /// <summary>7 × 8 and 8 × 7 count as the same fact.</summary>
    public (int Low, int High) Key => (Math.Min(Left, Right), Math.Max(Left, Right));

    public override string ToString() => $"{Left} × {Right}";
}

/// <summary>
/// The fact written in each open square. No two squares show the same fact while there are enough
/// facts to go round, and a square that gets a new fact never gets the one it just had.
/// </summary>
public sealed class FactSheet
{
    public const int LowestFactor = 2;

    private readonly Random _random;
    private readonly Dictionary<Cell, Fact> _facts = new();

    public FactSheet(Random random)
    {
        _random = random;
    }

    /// <summary>The biggest number in a fact, for example 12 for facts up to 12 × 12.</summary>
    public int HighestFactor { get; private set; } = 12;

    public Fact this[Cell cell] => _facts[cell];

    public IReadOnlyDictionary<Cell, Fact> All => _facts;

    /// <summary>Gives every square a fresh fact, for a new game.</summary>
    public void DealAll(IEnumerable<Cell> cells, int highestFactor)
    {
        HighestFactor = Math.Max(LowestFactor + 1, highestFactor);
        _facts.Clear();
        foreach (var cell in cells)
        {
            _facts[cell] = Draw(previous: null);
        }
    }

    /// <summary>Gives one square a new fact, different from the one it had.</summary>
    public void Replace(Cell cell)
    {
        var previous = _facts[cell];
        _facts.Remove(cell);
        _facts[cell] = Draw(previous);
    }

    private Fact Draw(Fact? previous)
    {
        var inUse = _facts.Values.Select(fact => fact.Key).ToHashSet();
        var choices = new List<(int Low, int High)>();
        var spares = new List<(int Low, int High)>();
        for (var low = LowestFactor; low <= HighestFactor; low++)
        {
            for (var high = low; high <= HighestFactor; high++)
            {
                if (previous?.Key == (low, high))
                {
                    continue;
                }

                (inUse.Contains((low, high)) ? spares : choices).Add((low, high));
            }
        }

        // With small facts on a 4 × 4 board there may not be enough to go round, so a repeat is allowed then.
        var pool = choices.Count > 0 ? choices : spares;
        var (a, b) = pool[_random.Next(pool.Count)];
        return _random.Next(2) == 0 ? new Fact(a, b) : new Fact(b, a);
    }
}
