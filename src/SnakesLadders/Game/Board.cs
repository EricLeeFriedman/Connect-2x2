namespace SnakesLadders.Game;

/// <summary>A snake (top to bottom) or a ladder (bottom to top) joining two squares.</summary>
public readonly record struct Jump(int From, int To)
{
    public bool IsLadder => To > From;

    public bool IsSnake => To < From;
}

/// <summary>Where a square sits on screen, counted from the top left, in squares.</summary>
public readonly record struct Spot(int Row, int Column);

/// <summary>
/// The classic 10 × 10 board, numbered 1 to 100 from the bottom left, snaking back and forth so
/// each row runs the opposite way to the one below it. Players start off the board, on square 0.
/// </summary>
public static class Board
{
    public const int Width = 10;
    public const int Start = 0;
    public const int Finish = Width * Width;

    /// <summary>Ladders go up, snakes come down. No square is the end of one and the start of another.</summary>
    public static readonly IReadOnlyList<Jump> Jumps =
    [
        new(3, 22), new(8, 26), new(20, 39), new(28, 55), new(36, 44), new(50, 69), new(63, 81), new(71, 92),
        new(17, 6), new(33, 12), new(47, 25), new(59, 40), new(66, 52), new(87, 58), new(94, 73), new(98, 79),
    ];

    private static readonly Dictionary<int, Jump> JumpsFrom = Jumps.ToDictionary(jump => jump.From);

    /// <summary>The snake or ladder that starts on this square, if any.</summary>
    public static Jump? JumpFrom(int square) => JumpsFrom.TryGetValue(square, out var jump) ? jump : null;

    /// <summary>Where a player ends up after landing on a square: up a ladder, down a snake, or right there.</summary>
    public static int Follow(int square) => JumpFrom(square)?.To ?? square;

    /// <summary>The squares 1 to 100.</summary>
    public static IEnumerable<int> Squares() => Enumerable.Range(1, Finish);

    /// <summary>The squares in screen order, top row first, left to right, for laying out the grid.</summary>
    public static IEnumerable<int> SquaresInScreenOrder()
    {
        for (var row = 0; row < Width; row++)
        {
            for (var column = 0; column < Width; column++)
            {
                yield return SquareAt(new Spot(row, column));
            }
        }
    }

    /// <summary>The square shown at this spot on screen.</summary>
    public static int SquareAt(Spot spot)
    {
        var fromBottom = Width - 1 - spot.Row;
        var along = fromBottom % 2 == 0 ? spot.Column : Width - 1 - spot.Column;
        return fromBottom * Width + along + 1;
    }

    /// <summary>Where a square is shown on screen.</summary>
    public static Spot SpotOf(int square)
    {
        if (square < 1 || square > Finish)
        {
            throw new ArgumentOutOfRangeException(nameof(square));
        }

        var fromBottom = (square - 1) / Width;
        var along = (square - 1) % Width;
        var column = fromBottom % 2 == 0 ? along : Width - 1 - along;
        return new Spot(Width - 1 - fromBottom, column);
    }
}
