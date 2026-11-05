namespace Checkers.Game;

public enum Side
{
    None = 0,
    Red = 1,
    Black = 2,
}

/// <summary>A square on the board. Row 0 is the top row (Black's side), column 0 is the leftmost column.</summary>
public readonly record struct Square(int Row, int Column)
{
    public bool IsOnBoard => Row >= 0 && Row < Board.Size && Column >= 0 && Column < Board.Size;

    /// <summary>Checkers is only played on the dark squares.</summary>
    public bool IsDark => (Row + Column) % 2 == 1;
}

public readonly record struct Piece(Side Side, bool IsKing = false);

/// <summary>One step or one jump. A jump carries the square of the piece it captures.</summary>
public readonly record struct Move(Square From, Square To, Square? Captured)
{
    public bool IsJump => Captured is not null;
}

/// <summary>
/// A standard 8 x 8 checkers board. Red starts on the bottom three rows and moves up;
/// Black starts on the top three rows and moves down. Kings move both ways.
/// </summary>
public sealed class Board
{
    public const int Size = 8;
    public const int StartingRows = 3;

    private readonly Piece?[,] _squares = new Piece?[Size, Size];

    public Piece? this[Square square] => _squares[square.Row, square.Column];

    public static IEnumerable<Square> DarkSquares()
    {
        for (var row = 0; row < Size; row++)
        {
            for (var column = 0; column < Size; column++)
            {
                var square = new Square(row, column);
                if (square.IsDark)
                {
                    yield return square;
                }
            }
        }
    }

    public IEnumerable<Square> EmptyDarkSquares() => DarkSquares().Where(square => this[square] is null);

    /// <summary>Row direction a plain piece moves in.</summary>
    public static int Forward(Side side) => side == Side.Red ? -1 : 1;

    /// <summary>The far row where a piece of this side is crowned.</summary>
    public static int CrownRow(Side side) => side == Side.Red ? 0 : Size - 1;

    public void Clear() => Array.Clear(_squares);

    public void SetUp()
    {
        Clear();
        foreach (var square in DarkSquares())
        {
            if (square.Row < StartingRows)
            {
                Put(square, new Piece(Side.Black));
            }
            else if (square.Row >= Size - StartingRows)
            {
                Put(square, new Piece(Side.Red));
            }
        }
    }

    public void Put(Square square, Piece? piece)
    {
        if (!square.IsOnBoard || !square.IsDark)
        {
            throw new ArgumentException($"Pieces only go on dark squares, not {square}.", nameof(square));
        }

        _squares[square.Row, square.Column] = piece;
    }

    public int Count(Side side) => DarkSquares().Count(square => this[square] is Piece piece && piece.Side == side);

    /// <summary>Every step and jump the piece on this square could make, ignoring the must-jump rule.</summary>
    public IEnumerable<Move> MovesFrom(Square from, bool jumpsOnly = false)
    {
        if (this[from] is not Piece piece)
        {
            yield break;
        }

        foreach (var (dr, dc) in Directions(piece))
        {
            var next = new Square(from.Row + dr, from.Column + dc);
            if (!next.IsOnBoard)
            {
                continue;
            }

            if (this[next] is not Piece other)
            {
                if (!jumpsOnly)
                {
                    yield return new Move(from, next, null);
                }

                continue;
            }

            if (other.Side == piece.Side)
            {
                continue;
            }

            var landing = new Square(from.Row + 2 * dr, from.Column + 2 * dc);
            if (landing.IsOnBoard && this[landing] is null)
            {
                yield return new Move(from, landing, next);
            }
        }
    }

    public List<Move> AllMoves(Side side) =>
        DarkSquares()
            .Where(square => this[square] is Piece piece && piece.Side == side)
            .SelectMany(square => MovesFrom(square))
            .ToList();

    /// <summary>Moves the piece, removes anything it jumped, and returns true when the piece was just crowned.</summary>
    public bool Apply(Move move)
    {
        if (this[move.From] is not Piece piece)
        {
            throw new InvalidOperationException($"There is no piece on {move.From}.");
        }

        var crowned = !piece.IsKing && move.To.Row == CrownRow(piece.Side);
        Put(move.From, null);
        if (move.Captured is Square captured)
        {
            Put(captured, null);
        }

        Put(move.To, piece with { IsKing = piece.IsKing || crowned });
        return crowned;
    }

    private static IEnumerable<(int Dr, int Dc)> Directions(Piece piece)
    {
        var forward = Forward(piece.Side);
        yield return (forward, -1);
        yield return (forward, 1);
        if (piece.IsKing)
        {
            yield return (-forward, -1);
            yield return (-forward, 1);
        }
    }
}
