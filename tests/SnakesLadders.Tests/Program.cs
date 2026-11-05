using SnakesLadders.Game;

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

// Rolls a chosen number and answers the fact on the square it lands on.
AnswerResult Play(GameSession session, int roll, bool answerRight = true)
{
    if (session.Roll(roll) != RollResult.Question)
    {
        throw new InvalidOperationException($"Rolling {roll} did not ask a question.");
    }

    var product = session.PendingFact!.Value.Product;
    return session.SubmitAnswer(answerRight ? product : product + 1);
}

// ---- The board ----
{
    Check(Board.Squares().Count() == 100, "the board has 100 squares");
    Check(Board.SpotOf(1) == new Spot(9, 0), "1 is at the bottom left");
    Check(Board.SpotOf(10) == new Spot(9, 9), "10 is at the bottom right");
    Check(Board.SpotOf(11) == new Spot(8, 9), "11 is right above 10");
    Check(Board.SpotOf(100) == new Spot(0, 0), "100 is at the top left");
    Check(Board.Squares().All(s => Board.SquareAt(Board.SpotOf(s)) == s), "every square maps to its spot and back");
    Check(Board.SquaresInScreenOrder().Distinct().Count() == 100, "the screen order shows each square once");

    var starts = Board.Jumps.Select(j => j.From).ToHashSet();
    Check(starts.Count == Board.Jumps.Count, "no square starts two snakes or ladders");
    Check(Board.Jumps.All(j => !starts.Contains(j.To)), "no snake or ladder ends where another starts");
    Check(Board.Jumps.All(j => j.From is > 1 and < 100 && j.To is >= 1 and <= 100), "snakes and ladders stay on the board");
    Check(Board.Jumps.Count(j => j.IsLadder) == 8 && Board.Jumps.Count(j => j.IsSnake) == 8, "8 ladders and 8 snakes");
}

// ---- Setting up ----
{
    var s = NewSession();
    Check(s.PlayerCount == 2 && s.CurrentPlayer == 0, "two players, the first goes first");
    Check(s.Players.All(p => s.Positions[p] == 0), "everyone starts off the board");
    Check(s.Facts.All.Count == 100, "every square has a fact");
    Check(Board.Squares().All(sq => s.Facts[sq].Key.Low >= FactSheet.LowestFactor && s.Facts[sq].Key.High <= 12), "facts are 2 to 12 by default");
    Check(Board.Squares().All(sq => Enumerable.Range(sq + 1, FactSheet.NearbyDistance).Where(o => o <= 100).All(o => s.Facts[o].Key != s.Facts[sq].Key)),
        "squares one roll apart never share a fact");
    var counts = s.Facts.All.Values.GroupBy(f => f.Key).Select(g => g.Count()).ToList();
    Check(counts.Max() - counts.Min() <= 2, "facts are spread evenly across the board");

    s.SetHighestFactor(5);
    Check(Board.Squares().All(sq => s.Facts[sq].Key.High <= 5), "facts up to 5 stay at 5 or under");
    Check(s.Facts.All.Count == 100, "small facts still fill every square");

    s.SetPlayerCount(4);
    Check(s.PlayerCount == 4 && s.Players.All(p => s.Positions[p] == 0), "four players all start off the board");
}

// ---- Moving ----
{
    var s = NewSession();
    Check(s.Roll(4) == RollResult.Question && s.Pending == 4, "rolling 4 from the start lands on square 4");
    Check(s.Roll(2) == RollResult.NotNow, "no second roll while a question is waiting");
    Check(s.SubmitAnswer(s.PendingFact!.Value.Product) == AnswerResult.Correct, "a right answer is correct");
    Check(s.Positions[0] == 4 && s.CurrentPlayer == 1, "the player moves to 4 and the turn passes");
    Check(s.LastMove == new Move(0, 0, 4, 4), "the move is remembered");
    Check(s.CorrectAnswers[0] == 1 && s.Attempts[0] == 1, "the fact is counted");
}

// ---- Snakes and ladders ----
{
    var s = NewSession();
    Play(s, 3);
    Check(s.Positions[0] == 22, "landing on the foot of a ladder climbs it");
    Check(s.LastMove is { Landed: 3, To: 22 } && s.LastMove.Value.Jump?.IsLadder == true, "the climb is shown as a ladder");

    Play(s, 6); // player 1 to 6
    Play(s, 6); // player 0 from 22 to 28, a ladder to 55
    Check(s.Positions[0] == 55, "another ladder");
    Play(s, 5); // player 1 from 6 to 11
    Play(s, 4); // player 0 from 55 to 59, a snake to 40
    Check(s.Positions[0] == 40, "landing on a snake's head slides down it");
    Check(s.LastMove?.Jump?.IsSnake == true, "the slide is shown as a snake");
}

// ---- Wrong answers ----
{
    var s = NewSession();
    s.Roll(3);
    var before = s.Facts[3];
    Check(s.SubmitAnswer(before.Product + 1) == AnswerResult.WrongTurnLost, "a wrong answer loses the turn by default");
    Check(s.Positions[0] == 0, "the player stays where they were, even at a ladder");
    Check(s.Facts[3].Key != before.Key, "the missed square gets a new fact");
    Check(s.CurrentPlayer == 1 && s.Pending is null, "play passes on");

    var t = NewSession();
    t.Rule = WrongAnswerRule.TryAgain;
    Check(Play(t, 5, answerRight: false) == AnswerResult.WrongTryAgain, "keep trying asks again");
    Check(t.CurrentPlayer == 0 && t.Pending == 5, "the same player keeps the same square");
    Check(t.SubmitAnswer(t.PendingFact!.Value.Product) == AnswerResult.Correct && t.Positions[0] == 5, "and can then get it right");
    Check(t.Attempts[0] == 2 && t.CorrectAnswers[0] == 1, "both tries are counted");
}

// ---- Winning ----
{
    var s = NewSession();
    s.Positions[0] = 96;
    Check(s.Roll(6) == RollResult.TooFar, "a roll past 100 doesn't move");
    Check(s.Positions[0] == 96 && s.CurrentPlayer == 1 && s.Pending is null, "the player stays and the turn passes");
    Check(s.Attempts[0] == 0, "no fact is asked for a roll that's too far");
    Play(s, 1);
    Play(s, 4);
    Check(s.Positions[0] == 100 && s.Winner == 0 && s.IsOver, "the exact roll to 100 wins");
    Check(s.Wins[0] == 1, "the win is counted");
    Check(s.Roll(1) == RollResult.NotNow, "no rolling once the game is over");

    s.NewGame();
    Check(s.CurrentPlayer == 1 && !s.IsOver && s.Wins[0] == 1 && s.Positions[0] == 0, "a new game swaps who starts and keeps the score");

    var t = NewSession();
    t.SetPlayerCount(3);
    Check(t.CurrentPlayer == 1, "three players: a new game passes the start along");
    Play(t, 1); Play(t, 1);
    Check(t.CurrentPlayer == 0, "and the turn goes round all three");
    Play(t, 1);
    Check(t.CurrentPlayer == 1, "back to the start");
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
return failures == 0 ? 0 : 1;
