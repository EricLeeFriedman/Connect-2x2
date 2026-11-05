namespace TicTacToe.Game;

public enum WrongAnswerRule
{
    /// <summary>A wrong answer ends the turn and the square stays open, with a new fact.</summary>
    LoseTurn,

    /// <summary>A wrong answer lets the same player answer again.</summary>
    TryAgain,
}

public enum AnswerResult
{
    NoQuestion,
    Correct,
    WrongTryAgain,
    WrongTurnLost,
}

/// <summary>
/// One sitting of times-table tic-tac-toe: whose turn it is, the square waiting on an answer,
/// and the running score across games.
/// </summary>
public sealed class GameSession
{
    /// <summary>The fact sizes a player can pick: facts up to 5 × 5, 10 × 10, or 12 × 12.</summary>
    public static readonly int[] FactLimits = [5, 10, 12];

    public GameSession(Random? random = null)
    {
        Facts = new FactSheet(random ?? new Random());
        StartingPlayer = Mark.X;
        Reset();
    }

    public Board Board { get; } = new();

    public FactSheet Facts { get; }

    /// <summary>3 for 3 × 3, 4 for 4 × 4. Changing it starts a new game.</summary>
    public int Size { get; private set; } = Board.SmallSize;

    /// <summary>The biggest number in a fact. Changing it starts a new game.</summary>
    public int HighestFactor { get; private set; } = 12;

    public Mark StartingPlayer { get; private set; }

    public Mark CurrentPlayer { get; private set; }

    /// <summary>The square waiting on an answer to its fact.</summary>
    public Cell? Pending { get; private set; }

    public Cell? LastClaimed { get; private set; }

    public Mark Winner { get; private set; }

    public Cell[]? WinningLine { get; private set; }

    public bool IsDraw { get; private set; }

    public bool IsOver => Winner != Mark.None || IsDraw;

    public WrongAnswerRule Rule { get; set; } = WrongAnswerRule.LoseTurn;

    public Dictionary<Mark, int> Wins { get; } = new() { [Mark.X] = 0, [Mark.O] = 0 };

    public int Draws { get; private set; }

    public Dictionary<Mark, int> Attempts { get; } = new() { [Mark.X] = 0, [Mark.O] = 0 };

    public Dictionary<Mark, int> CorrectAnswers { get; } = new() { [Mark.X] = 0, [Mark.O] = 0 };

    public static Mark Opponent(Mark mark) => mark == Mark.X ? Mark.O : Mark.X;

    public bool IsOnWinningLine(Cell cell) => WinningLine is Cell[] line && line.Contains(cell);

    /// <summary>Picks an open square; the question is the fact in it.</summary>
    public bool Choose(Cell cell)
    {
        if (IsOver || Pending is not null || !Board.IsEmpty(cell))
        {
            return false;
        }

        Pending = cell;
        return true;
    }

    public void CancelQuestion() => Pending = null;

    public Fact? PendingFact => Pending is Cell cell ? Facts[cell] : null;

    public AnswerResult SubmitAnswer(int answer)
    {
        if (Pending is not Cell cell)
        {
            return AnswerResult.NoQuestion;
        }

        var mover = CurrentPlayer;
        Attempts[mover]++;

        if (answer == Facts[cell].Product)
        {
            CorrectAnswers[mover]++;
            Pending = null;
            Board.Put(cell, mover);
            LastClaimed = cell;

            WinningLine = Board.WinningLine(mover);
            if (WinningLine is not null)
            {
                Winner = mover;
                Wins[mover]++;
            }
            else if (Board.IsFull)
            {
                IsDraw = true;
                Draws++;
            }
            else
            {
                CurrentPlayer = Opponent(mover);
            }

            return AnswerResult.Correct;
        }

        if (Rule == WrongAnswerRule.TryAgain)
        {
            return AnswerResult.WrongTryAgain;
        }

        // The answer is shown after a miss, so the square gets a new fact before the other player can pick it.
        Pending = null;
        Facts.Replace(cell);
        CurrentPlayer = Opponent(mover);
        return AnswerResult.WrongTurnLost;
    }

    /// <summary>Starts a fresh board and lets the other player go first this time. Scores are kept.</summary>
    public void NewGame()
    {
        StartingPlayer = Opponent(StartingPlayer);
        Reset();
    }

    /// <summary>Switches between 3 × 3 and 4 × 4 and starts a new game.</summary>
    public void SetSize(int size)
    {
        if (size == Size)
        {
            return;
        }

        Board.Clear(size);
        Size = size;
        NewGame();
    }

    /// <summary>Picks how big the facts get and starts a new game.</summary>
    public void SetHighestFactor(int highest)
    {
        if (highest == HighestFactor)
        {
            return;
        }

        HighestFactor = highest;
        NewGame();
    }

    private void Reset()
    {
        Board.Clear(Size);
        Facts.DealAll(Board.Cells(), HighestFactor);
        CurrentPlayer = StartingPlayer;
        Pending = null;
        LastClaimed = null;
        Winner = Mark.None;
        WinningLine = null;
        IsDraw = false;
    }
}
