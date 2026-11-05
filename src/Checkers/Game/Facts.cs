namespace Checkers.Game;

/// <summary>A times-table fact such as 7 × 8. The order the numbers are shown in is random.</summary>
public readonly record struct Fact(int Left, int Right)
{
    public int Product => Left * Right;

    /// <summary>7 × 8 and 8 × 7 count as the same fact.</summary>
    public (int Low, int High) Key => (Math.Min(Left, Right), Math.Max(Left, Right));

    public override string ToString() => $"{Left} × {Right}";
}

/// <summary>
/// The fact written in each dark square. No two squares show the same fact at once,
/// and a square that gets a new fact never gets the one it just had.
/// </summary>
public sealed class FactBoard
{
    public const int LowestFactor = 3;
    public const int HighestFactor = 12;

    private readonly Random _random;
    private readonly Dictionary<Square, Fact> _facts = new();

    public FactBoard(Random random)
    {
        _random = random;
    }

    public Fact this[Square square] => _facts[square];

    public IReadOnlyDictionary<Square, Fact> All => _facts;

    /// <summary>Gives every dark square a fresh fact, for a new game.</summary>
    public void DealAll()
    {
        _facts.Clear();
        foreach (var square in Board.DarkSquares())
        {
            _facts[square] = Draw(previous: null);
        }
    }

    /// <summary>Gives one square a new fact, different from the one it had.</summary>
    public void Replace(Square square) => Reshuffle([square]);

    /// <summary>Gives each of these squares a new fact, each different from the one it had.</summary>
    public void Reshuffle(IEnumerable<Square> squares)
    {
        var old = squares.ToDictionary(square => square, square => _facts[square]);
        foreach (var square in old.Keys)
        {
            _facts.Remove(square);
        }

        foreach (var (square, previous) in old)
        {
            _facts[square] = Draw(previous);
        }
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

        // There are 55 facts and only 32 squares, so a free one is always left; the spares are just a safety net.
        var pool = choices.Count > 0 ? choices : spares;
        var (a, b) = pool[_random.Next(pool.Count)];
        return _random.Next(2) == 0 ? new Fact(a, b) : new Fact(b, a);
    }
}
