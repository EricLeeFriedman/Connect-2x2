namespace SnakesLadders.Game;

public enum WrongAnswerRule
{
    /// <summary>A wrong answer ends the turn: the player stays put and the square gets a new fact.</summary>
    LoseTurn,

    /// <summary>A wrong answer lets the same player answer again.</summary>
    TryAgain,
}

public enum RollResult
{
    /// <summary>Nothing happened: the game is over or a question is waiting.</summary>
    NotNow,

    /// <summary>The roll lands on a square, and its fact is now the question.</summary>
    Question,

    /// <summary>The roll would go past 100. The player stays put and the turn passes.</summary>
    TooFar,
}

public enum AnswerResult
{
    NoQuestion,
    Correct,
    WrongTryAgain,
    WrongTurnLost,
}

/// <summary>One completed move: where the player was, the square they landed on, and where they ended up.</summary>
public readonly record struct Move(int Player, int From, int Landed, int To)
{
    public Jump? Jump => Board.JumpFrom(Landed);
}

/// <summary>
/// One sitting of times-table snakes and ladders: whose turn it is, the last roll, the square waiting on
/// an answer, where everyone is, and the running score across games.
/// </summary>
public sealed class GameSession
{
    public const int MinPlayers = 2;
    public const int MaxPlayers = 4;
    public const int DieSides = 6;

    /// <summary>The fact sizes a player can pick: facts up to 5 × 5, 10 × 10, or 12 × 12.</summary>
    public static readonly int[] FactLimits = [5, 10, 12];

    private readonly Random _random;

    public GameSession(Random? random = null)
    {
        _random = random ?? new Random();
        Facts = new FactSheet(_random);
        StartingPlayer = 0;
        Reset();
    }

    public FactSheet Facts { get; }

    /// <summary>How many people are playing, 2 to 4. Changing it starts a new game.</summary>
    public int PlayerCount { get; private set; } = MinPlayers;

    public IEnumerable<int> Players => Enumerable.Range(0, PlayerCount);

    /// <summary>The biggest number in a fact. Changing it starts a new game.</summary>
    public int HighestFactor { get; private set; } = 12;

    /// <summary>Each player's square. 0 is the start, off the board.</summary>
    public int[] Positions { get; private set; } = new int[MaxPlayers];

    public int StartingPlayer { get; private set; }

    public int CurrentPlayer { get; private set; }

    /// <summary>The number on the die from the latest roll, or null before the first roll.</summary>
    public int? LastRoll { get; private set; }

    /// <summary>The square the current roll lands on, waiting on an answer to its fact.</summary>
    public int? Pending { get; private set; }

    public Fact? PendingFact => Pending is int square ? Facts[square] : null;

    /// <summary>The latest move a player actually made.</summary>
    public Move? LastMove { get; private set; }

    /// <summary>The winner's player number, or null while the game is on.</summary>
    public int? Winner { get; private set; }

    public bool IsOver => Winner is not null;

    public WrongAnswerRule Rule { get; set; } = WrongAnswerRule.LoseTurn;

    public int[] Wins { get; } = new int[MaxPlayers];

    public int[] Attempts { get; } = new int[MaxPlayers];

    public int[] CorrectAnswers { get; } = new int[MaxPlayers];

    public bool CanRoll => !IsOver && Pending is null;

    public int Next(int player) => (player + 1) % PlayerCount;

    /// <summary>Rolls the die for the current player. Pass a number to choose the roll (for tests).</summary>
    public RollResult Roll(int? value = null)
    {
        if (!CanRoll)
        {
            return RollResult.NotNow;
        }

        var roll = value ?? _random.Next(1, DieSides + 1);
        if (roll < 1 || roll > DieSides)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A die shows 1 to 6.");
        }

        LastRoll = roll;
        var target = Positions[CurrentPlayer] + roll;
        if (target > Board.Finish)
        {
            // Normal rules: you need the exact number to reach 100.
            CurrentPlayer = Next(CurrentPlayer);
            return RollResult.TooFar;
        }

        Pending = target;
        return RollResult.Question;
    }

    public AnswerResult SubmitAnswer(int answer)
    {
        if (Pending is not int square)
        {
            return AnswerResult.NoQuestion;
        }

        var mover = CurrentPlayer;
        Attempts[mover]++;

        if (answer == Facts[square].Product)
        {
            CorrectAnswers[mover]++;
            Pending = null;
            var end = Board.Follow(square);
            LastMove = new Move(mover, Positions[mover], square, end);
            Positions[mover] = end;

            if (end == Board.Finish)
            {
                Winner = mover;
                Wins[mover]++;
            }
            else
            {
                CurrentPlayer = Next(mover);
            }

            return AnswerResult.Correct;
        }

        if (Rule == WrongAnswerRule.TryAgain)
        {
            return AnswerResult.WrongTryAgain;
        }

        // The answer is shown after a miss, so the square gets a new fact before anyone else lands there.
        Pending = null;
        Facts.Replace(square);
        CurrentPlayer = Next(mover);
        return AnswerResult.WrongTurnLost;
    }

    /// <summary>Everyone back to the start, and the next player along goes first. Scores are kept.</summary>
    public void NewGame()
    {
        StartingPlayer = Next(StartingPlayer);
        Reset();
    }

    /// <summary>Picks how many people are playing and starts a new game.</summary>
    public void SetPlayerCount(int count)
    {
        if (count < MinPlayers || count > MaxPlayers)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "2 to 4 players.");
        }

        if (count == PlayerCount)
        {
            return;
        }

        PlayerCount = count;
        StartingPlayer %= PlayerCount;
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
        Facts.DealAll(Board.Squares(), HighestFactor);
        Positions = new int[MaxPlayers];
        CurrentPlayer = StartingPlayer;
        LastRoll = null;
        Pending = null;
        LastMove = null;
        Winner = null;
    }
}
