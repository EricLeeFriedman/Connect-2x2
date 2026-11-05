namespace TicTacToe.Game;

public enum Mark
{
    None,
    X,
    O,
}

/// <summary>A square on the board, counted from the top left.</summary>
public readonly record struct Cell(int Row, int Column);

/// <summary>
/// A square tic-tac-toe grid, 3 × 3 or 4 × 4. A line is a whole row, column, or long diagonal,
/// so three in a row wins on 3 × 3 and four in a row wins on 4 × 4.
/// </summary>
public sealed class Board
{
    public const int SmallSize = 3;
    public const int LargeSize = 4;

    private Mark[,] _marks = new Mark[SmallSize, SmallSize];

    public Board(int size = SmallSize)
    {
        Clear(size);
    }

    public int Size { get; private set; }

    public Mark this[Cell cell] => _marks[cell.Row, cell.Column];

    /// <summary>Empties the board, at a new size if one is given.</summary>
    public void Clear(int size)
    {
        if (size != SmallSize && size != LargeSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "The board is 3 × 3 or 4 × 4.");
        }

        Size = size;
        _marks = new Mark[size, size];
    }

    public bool IsInside(Cell cell) => cell.Row >= 0 && cell.Row < Size && cell.Column >= 0 && cell.Column < Size;

    public bool IsEmpty(Cell cell) => IsInside(cell) && this[cell] == Mark.None;

    public void Put(Cell cell, Mark mark) => _marks[cell.Row, cell.Column] = mark;

    public IEnumerable<Cell> Cells()
    {
        for (var row = 0; row < Size; row++)
        {
            for (var column = 0; column < Size; column++)
            {
                yield return new Cell(row, column);
            }
        }
    }

    public IEnumerable<Cell> EmptyCells() => Cells().Where(IsEmpty);

    public bool IsFull => !EmptyCells().Any();

    /// <summary>Every row, column, and the two long diagonals.</summary>
    public IEnumerable<Cell[]> Lines()
    {
        var indexes = Enumerable.Range(0, Size).ToArray();
        foreach (var i in indexes)
        {
            yield return indexes.Select(j => new Cell(i, j)).ToArray();
            yield return indexes.Select(j => new Cell(j, i)).ToArray();
        }

        yield return indexes.Select(j => new Cell(j, j)).ToArray();
        yield return indexes.Select(j => new Cell(j, Size - 1 - j)).ToArray();
    }

    /// <summary>A full line of this mark, or null.</summary>
    public Cell[]? WinningLine(Mark mark) =>
        mark == Mark.None ? null : Lines().FirstOrDefault(line => line.All(cell => this[cell] == mark));
}
