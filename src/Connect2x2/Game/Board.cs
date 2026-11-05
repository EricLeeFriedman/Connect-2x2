namespace Connect2x2.Game;

public enum Player
{
    None = 0,
    Red = 1,
    Yellow = 2,
}

/// <summary>A spot on the board. Row 0 is the bottom row, column 0 is the leftmost column.</summary>
public readonly record struct Cell(int Row, int Column);

/// <summary>
/// A classic 7 x 6 Connect 4 grid where every spot is also a multiplication fact:
/// the columns are numbered 6 to 12 along the bottom and the rows 5 to 10 up the side.
/// </summary>
public sealed class Board
{
    public const int Columns = 7;
    public const int Rows = 6;
    public const int ConnectLength = 4;

    /// <summary>Number printed under the leftmost column (6, 7, ... 12).</summary>
    public const int FirstColumnFactor = 6;

    /// <summary>Number printed next to the bottom row (5, 6, ... 10, counting upward).</summary>
    public const int FirstRowFactor = 5;

    private static readonly (int Dr, int Dc)[] Directions = [(0, 1), (1, 0), (1, 1), (1, -1)];

    private readonly Player[,] _cells = new Player[Rows, Columns];

    public Player this[int row, int column] => _cells[row, column];

    public static int ColumnFactor(int column) => FirstColumnFactor + column;

    public static int RowFactor(int row) => FirstRowFactor + row;

    public static int Product(Cell cell) => RowFactor(cell.Row) * ColumnFactor(cell.Column);

    /// <summary>Where a piece dropped into this column would land, or null when the column is full.</summary>
    public int? LowestEmptyRow(int column)
    {
        if (column < 0 || column >= Columns)
        {
            return null;
        }

        for (var row = 0; row < Rows; row++)
        {
            if (_cells[row, column] == Player.None)
            {
                return row;
            }
        }

        return null;
    }

    public bool IsFull
    {
        get
        {
            for (var column = 0; column < Columns; column++)
            {
                if (_cells[Rows - 1, column] == Player.None)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void Place(Cell cell, Player player)
    {
        if (_cells[cell.Row, cell.Column] != Player.None)
        {
            throw new InvalidOperationException($"Spot {cell} is already taken.");
        }

        _cells[cell.Row, cell.Column] = player;
    }

    /// <summary>
    /// Returns every cell in a run of four or more that passes through <paramref name="from"/>,
    /// or null when that piece did not complete a line.
    /// </summary>
    public IReadOnlyList<Cell>? FindLine(Cell from)
    {
        var player = _cells[from.Row, from.Column];
        if (player == Player.None)
        {
            return null;
        }

        foreach (var (dr, dc) in Directions)
        {
            var line = new List<Cell> { from };
            Walk(from, dr, dc, player, line);
            Walk(from, -dr, -dc, player, line);
            if (line.Count >= ConnectLength)
            {
                return line;
            }
        }

        return null;
    }

    public void Clear() => Array.Clear(_cells);

    private void Walk(Cell start, int dr, int dc, Player player, List<Cell> into)
    {
        var row = start.Row + dr;
        var column = start.Column + dc;
        while (row >= 0 && row < Rows && column >= 0 && column < Columns && _cells[row, column] == player)
        {
            into.Add(new Cell(row, column));
            row += dr;
            column += dc;
        }
    }
}
