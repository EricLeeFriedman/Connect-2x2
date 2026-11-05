namespace Connect2x2.Game;

public enum WrongAnswerRule
{
    /// <summary>A wrong answer ends your turn without placing a piece.</summary>
    LoseTurn,

    /// <summary>A wrong answer lets you keep trying the same fact.</summary>
    TryAgain,
}

public enum AnswerResult
{
    NoQuestion,
    Correct,
    WrongTryAgain,
    WrongTurnLost,
}

/// <summary>All the rules of a two-player game: whose turn it is, the pending question, wins and stats.</summary>
public sealed class GameSession
{
    public Board Board { get; } = new();

    public Player CurrentPlayer { get; private set; } = Player.Red;

    public Player StartingPlayer { get; private set; } = Player.Red;

    /// <summary>The spot the current player picked and must now answer for.</summary>
    public Cell? Pending { get; private set; }

    public Cell? LastPlaced { get; private set; }

    public Player Winner { get; private set; } = Player.None;

    public IReadOnlyList<Cell> WinningCells { get; private set; } = [];

    public bool IsDraw { get; private set; }

    public bool IsOver => Winner != Player.None || IsDraw;

    public WrongAnswerRule Rule { get; set; } = WrongAnswerRule.LoseTurn;

    public Dictionary<Player, int> Wins { get; } = new() { [Player.Red] = 0, [Player.Yellow] = 0 };

    public Dictionary<Player, int> CorrectAnswers { get; } = new() { [Player.Red] = 0, [Player.Yellow] = 0 };

    public Dictionary<Player, int> Attempts { get; } = new() { [Player.Red] = 0, [Player.Yellow] = 0 };

    public static Player Opponent(Player player) => player == Player.Red ? Player.Yellow : Player.Red;

    /// <summary>Picks a column; the question is for the lowest open spot in it, like dropping a real piece.</summary>
    public bool TrySelectColumn(int column)
    {
        if (IsOver || Pending is not null)
        {
            return false;
        }

        var row = Board.LowestEmptyRow(column);
        if (row is null)
        {
            return false;
        }

        Pending = new Cell(row.Value, column);
        return true;
    }

    public void CancelQuestion() => Pending = null;

    public AnswerResult SubmitAnswer(int answer)
    {
        if (Pending is not Cell cell)
        {
            return AnswerResult.NoQuestion;
        }

        Attempts[CurrentPlayer]++;

        if (answer == Board.Product(cell))
        {
            CorrectAnswers[CurrentPlayer]++;
            Board.Place(cell, CurrentPlayer);
            LastPlaced = cell;
            Pending = null;

            var line = Board.FindLine(cell);
            if (line is not null)
            {
                Winner = CurrentPlayer;
                WinningCells = line;
                Wins[CurrentPlayer]++;
            }
            else if (Board.IsFull)
            {
                IsDraw = true;
            }
            else
            {
                CurrentPlayer = Opponent(CurrentPlayer);
            }

            return AnswerResult.Correct;
        }

        if (Rule == WrongAnswerRule.TryAgain)
        {
            return AnswerResult.WrongTryAgain;
        }

        Pending = null;
        CurrentPlayer = Opponent(CurrentPlayer);
        return AnswerResult.WrongTurnLost;
    }

    /// <summary>Clears the board and lets the other player go first this time. Scores are kept.</summary>
    public void NewGame()
    {
        Board.Clear();
        StartingPlayer = Opponent(StartingPlayer);
        CurrentPlayer = StartingPlayer;
        Pending = null;
        LastPlaced = null;
        Winner = Player.None;
        WinningCells = [];
        IsDraw = false;
    }
}
