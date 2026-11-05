using TicTacToe.Game;

var failures = 0;

void Check(bool condition, string what)
{
    Console.WriteLine($"{(condition ? "PASS" : "FAIL")}  {what}");
    if (!condition)
    {
        failures++;
    }
}

GameSession NewSession(int seed = 1) => new(new Random(seed));

// Picks a square and answers its fact.
AnswerResult Claim(GameSession session, int row, int column, bool answerRight = true)
{
    if (!session.Choose(new Cell(row, column)))
    {
        throw new InvalidOperationException($"Could not pick ({row}, {column}).");
    }

    var product = session.PendingFact!.Value.Product;
    return session.SubmitAnswer(answerRight ? product : product + 1);
}

// ---- Setting up ----
{
    var s = NewSession();
    Check(s.Size == 3 && s.Board.Cells().Count() == 9, "the default board is 3 x 3");
    Check(s.CurrentPlayer == Mark.X, "X goes first");
    var keys = s.Board.Cells().Select(c => s.Facts[c].Key).ToList();
    Check(keys.Distinct().Count() == 9, "no two squares share a fact");
    Check(keys.All(k => k.Low >= FactSheet.LowestFactor && k.High <= 12), "facts are 2 to 12 by default");

    s.SetSize(Board.LargeSize);
    Check(s.Size == 4 && s.Board.Cells().Count() == 16 && s.Facts.All.Count == 16, "4 x 4 has 16 squares, each with a fact");
    Check(s.CurrentPlayer == Mark.O, "changing the board starts a new game with the other player first");

    s.SetHighestFactor(5);
    Check(s.Board.Cells().All(c => s.Facts[c].Key.High <= 5), "facts up to 5 stay at 5 or under");
    Check(s.Facts.All.Count == 16, "4 x 4 with small facts still fills every square");
}

// ---- Claiming ----
{
    var s = NewSession();
    Check(Claim(s, 0, 0) == AnswerResult.Correct, "a right answer is correct");
    Check(s.Board[new Cell(0, 0)] == Mark.X, "a right answer claims the square");
    Check(s.CurrentPlayer == Mark.O, "then it is O's turn");
    Check(!s.Choose(new Cell(0, 0)), "a claimed square can't be picked again");
    Check(s.CorrectAnswers[Mark.X] == 1 && s.Attempts[Mark.X] == 1, "X's facts are counted");
}

// ---- Wrong answers ----
{
    var s = NewSession();
    var before = s.Facts[new Cell(1, 1)];
    Check(Claim(s, 1, 1, answerRight: false) == AnswerResult.WrongTurnLost, "a wrong answer loses the turn by default");
    Check(s.Board[new Cell(1, 1)] == Mark.None, "the missed square stays open");
    Check(s.Facts[new Cell(1, 1)].Key != before.Key, "the missed square gets a new fact");
    Check(s.CurrentPlayer == Mark.O && s.Pending is null, "play passes to O");

    var t = NewSession();
    t.Rule = WrongAnswerRule.TryAgain;
    Check(Claim(t, 1, 1, answerRight: false) == AnswerResult.WrongTryAgain, "keep trying asks again");
    Check(t.CurrentPlayer == Mark.X && t.Pending == new Cell(1, 1), "the same player keeps the same square");
    Check(t.SubmitAnswer(t.PendingFact!.Value.Product) == AnswerResult.Correct, "and can then get it right");
    Check(t.Attempts[Mark.X] == 2 && t.CorrectAnswers[Mark.X] == 1, "both tries are counted");
}

// ---- Winning ----
{
    // X: top row. O: middle row (two).
    var s = NewSession();
    Claim(s, 0, 0); Claim(s, 1, 0);
    Claim(s, 0, 1); Claim(s, 1, 1);
    Claim(s, 0, 2);
    Check(s.Winner == Mark.X && s.IsOver && s.Wins[Mark.X] == 1, "three across wins");
    Check(s.WinningLine is { Length: 3 } && s.IsOnWinningLine(new Cell(0, 2)), "the winning line is known");
    Check(!s.Choose(new Cell(2, 2)), "no moves once the game is over");

    s.NewGame();
    Check(s.CurrentPlayer == Mark.O && !s.IsOver && s.Wins[Mark.X] == 1, "a new game swaps who starts and keeps the score");

    // O (starting) takes the anti-diagonal.
    Claim(s, 0, 2); Claim(s, 0, 0);
    Claim(s, 1, 1); Claim(s, 0, 1);
    Claim(s, 2, 0);
    Check(s.Winner == Mark.O, "corner to corner wins");

    var d = NewSession();
    d.SetSize(Board.LargeSize);
    // O starts this game. O goes down column 0 while X goes down column 3.
    Claim(d, 0, 0); Claim(d, 0, 3);
    Claim(d, 1, 0); Claim(d, 1, 3);
    Claim(d, 2, 0);
    Check(!d.IsOver, "three in a row is not enough on 4 x 4");
    Claim(d, 2, 3);
    Claim(d, 3, 0);
    Check(d.Winner == Mark.O, "four down wins on 4 x 4");
}

// ---- Draws ----
{
    // X O X / X O O / O X X : nobody has a line.
    var s = NewSession();
    Claim(s, 0, 0); Claim(s, 0, 1);
    Claim(s, 0, 2); Claim(s, 1, 1);
    Claim(s, 1, 0); Claim(s, 2, 0);
    Claim(s, 2, 1); Claim(s, 1, 2);
    Claim(s, 2, 2);
    Check(s.IsDraw && s.Winner == Mark.None && s.Draws == 1, "a full board with no line is a draw");
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
return failures == 0 ? 0 : 1;
