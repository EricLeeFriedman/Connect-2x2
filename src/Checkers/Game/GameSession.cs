namespace Checkers.Game;

public enum WrongAnswerRule
{
    /// <summary>A wrong answer ends the turn without moving.</summary>
    LoseTurn,

    /// <summary>A wrong answer lets the same player answer again.</summary>
    TryAgain,
}

public enum AnswerResult
{
    NoQuestion,
    Correct,
    CorrectKeepJumping,
    WrongTryAgain,
    WrongTurnLost,
}

/// <summary>
/// One sitting of times-table checkers: whose turn it is, which piece is picked, the question for the
/// square it is moving to, and the running score across games.
/// </summary>
public sealed class GameSession
{
    /// <summary>Every this many turns, all the facts on the open squares change.</summary>
    public const int ShuffleEvery = 5;

    /// <summary>The game is a draw after this many turns in a row with no capture and no new king.</summary>
    public const int QuietTurnsForDraw = 50;

    private bool _progressThisTurn;

    public GameSession(Random? random = null)
    {
        Facts = new FactBoard(random ?? new Random());
        StartingPlayer = Side.Red;
        Reset();
    }

    public Board Board { get; } = new();

    public FactBoard Facts { get; }

    public Side StartingPlayer { get; private set; }

    public Side CurrentPlayer { get; private set; }

    /// <summary>The piece the current player has picked up.</summary>
    public Square? Selected { get; private set; }

    /// <summary>Set in the middle of a multi-jump: this piece must keep jumping.</summary>
    public Square? JumpingFrom { get; private set; }

    /// <summary>The move waiting on an answer to the fact in its landing square.</summary>
    public Move? Pending { get; private set; }

    public Move? LastMove { get; private set; }

    public Side Winner { get; private set; }

    public bool IsDraw { get; private set; }

    public bool IsOver => Winner != Side.None || IsDraw;

    public int TurnsTaken { get; private set; }

    public int TurnsUntilShuffle => ShuffleEvery - TurnsTaken % ShuffleEvery;

    /// <summary>How many times the facts have been shuffled this game.</summary>
    public int ShuffleCount { get; private set; }

    public int QuietTurns { get; private set; }

    public WrongAnswerRule Rule { get; set; } = WrongAnswerRule.LoseTurn;

    /// <summary>Standard checkers: if you can jump, you must.</summary>
    public bool MustJump { get; set; } = true;

    public Dictionary<Side, int> Wins { get; } = new() { [Side.Red] = 0, [Side.Black] = 0 };

    public Dictionary<Side, int> Attempts { get; } = new() { [Side.Red] = 0, [Side.Black] = 0 };

    public Dictionary<Side, int> CorrectAnswers { get; } = new() { [Side.Red] = 0, [Side.Black] = 0 };

    public static Side Opponent(Side side) => side == Side.Red ? Side.Black : Side.Red;

    /// <summary>Every move the current player may make right now.</summary>
    public List<Move> LegalMoves()
    {
        if (IsOver)
        {
            return [];
        }

        if (JumpingFrom is Square jumper)
        {
            return Board.MovesFrom(jumper, jumpsOnly: true).ToList();
        }

        var moves = Board.AllMoves(CurrentPlayer);
        return MustJump && moves.Any(move => move.IsJump) ? moves.Where(move => move.IsJump).ToList() : moves;
    }

    public bool HasJump() => Board.AllMoves(CurrentPlayer).Any(move => move.IsJump);

    public bool CanMove(Square square) => LegalMoves().Any(move => move.From == square);

    /// <summary>When jumps are optional, a player part way through a multi-jump may stop.</summary>
    public bool CanEndTurnEarly => !IsOver && JumpingFrom is not null && !MustJump && Pending is null;

    /// <summary>Picks up one of the current player's pieces that can move.</summary>
    public bool Select(Square square)
    {
        if (IsOver || Pending is not null || JumpingFrom is not null || !CanMove(square))
        {
            return false;
        }

        Selected = square;
        return true;
    }

    public void Deselect()
    {
        if (JumpingFrom is null && Pending is null)
        {
            Selected = null;
        }
    }

    /// <summary>Picks where the selected piece goes; the question is the fact in that square.</summary>
    public bool TryChooseDestination(Square to)
    {
        if (IsOver || Pending is not null || Selected is not Square from)
        {
            return false;
        }

        foreach (var move in LegalMoves())
        {
            if (move.From == from && move.To == to)
            {
                Pending = move;
                return true;
            }
        }

        return false;
    }

    public void CancelQuestion() => Pending = null;

    public Fact? PendingFact => Pending is Move move ? Facts[move.To] : null;

    public AnswerResult SubmitAnswer(int answer)
    {
        if (Pending is not Move move)
        {
            return AnswerResult.NoQuestion;
        }

        Attempts[CurrentPlayer]++;

        if (answer == Facts[move.To].Product)
        {
            CorrectAnswers[CurrentPlayer]++;
            Pending = null;
            LastMove = move;

            var crowned = Board.Apply(move);

            // The squares that just opened up get new facts.
            Facts.Replace(move.From);
            if (move.Captured is Square captured)
            {
                Facts.Replace(captured);
            }

            if (move.IsJump || crowned)
            {
                _progressThisTurn = true;
            }

            // After a jump the same piece keeps going if it can, unless it was just crowned.
            if (move.IsJump && !crowned && Board.MovesFrom(move.To, jumpsOnly: true).Any())
            {
                JumpingFrom = move.To;
                Selected = move.To;
                return AnswerResult.CorrectKeepJumping;
            }

            EndTurn();
            return AnswerResult.Correct;
        }

        if (Rule == WrongAnswerRule.TryAgain)
        {
            return AnswerResult.WrongTryAgain;
        }

        EndTurn();
        return AnswerResult.WrongTurnLost;
    }

    public void EndTurnEarly()
    {
        if (CanEndTurnEarly)
        {
            EndTurn();
        }
    }

    /// <summary>Starts a fresh board and lets the other player go first this time. Scores are kept.</summary>
    public void NewGame()
    {
        StartingPlayer = Opponent(StartingPlayer);
        Reset();
    }

    private void EndTurn()
    {
        var mover = CurrentPlayer;
        Selected = null;
        JumpingFrom = null;
        Pending = null;

        QuietTurns = _progressThisTurn ? 0 : QuietTurns + 1;
        _progressThisTurn = false;

        TurnsTaken++;
        CurrentPlayer = Opponent(mover);

        if (TurnsTaken % ShuffleEvery == 0)
        {
            Facts.Reshuffle(Board.EmptyDarkSquares().ToList());
            ShuffleCount++;
        }

        if (LegalMoves().Count == 0)
        {
            Winner = mover;
            Wins[mover]++;
        }
        else if (QuietTurns >= QuietTurnsForDraw)
        {
            IsDraw = true;
        }
    }

    private void Reset()
    {
        Board.SetUp();
        Facts.DealAll();
        CurrentPlayer = StartingPlayer;
        Selected = null;
        JumpingFrom = null;
        Pending = null;
        LastMove = null;
        Winner = Side.None;
        IsDraw = false;
        TurnsTaken = 0;
        ShuffleCount = 0;
        QuietTurns = 0;
        _progressThisTurn = false;
    }
}
