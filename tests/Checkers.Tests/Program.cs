using Checkers.Game;

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

// Moves the selected piece, answering the fact in the landing square.
AnswerResult Play(GameSession session, Square from, Square to, bool answerRight = true)
{
    if (!session.Select(from) && session.JumpingFrom != from)
    {
        throw new InvalidOperationException($"Could not pick up {from}.");
    }

    if (!session.TryChooseDestination(to))
    {
        throw new InvalidOperationException($"{from} cannot move to {to}.");
    }

    var product = session.PendingFact!.Value.Product;
    return session.SubmitAnswer(answerRight ? product : product + 1);
}

// Clears the board, then places pieces. "R"/"B" are plain pieces, "RK"/"BK" kings.
GameSession Position(Side toMove, params (int Row, int Column, string Kind)[] pieces)
{
    var session = NewSession();
    if (session.CurrentPlayer != toMove)
    {
        session.NewGame();
    }

    session.Board.Clear();
    foreach (var (row, column, kind) in pieces)
    {
        var side = kind.StartsWith('R') ? Side.Red : Side.Black;
        session.Board.Put(new Square(row, column), new Piece(side, kind.EndsWith('K')));
    }

    return session;
}

// ---- Setting up ----
{
    var s = NewSession();
    Check(s.Board.Count(Side.Red) == 12 && s.Board.Count(Side.Black) == 12, "each side starts with 12 pieces");
    Check(s.CurrentPlayer == Side.Red, "Red goes first in the first game");
    var facts = s.Facts.All;
    Check(facts.Count == 32, "all 32 dark squares have a fact");
    Check(facts.Values.Select(f => f.Key).Distinct().Count() == 32, "no two squares share a fact");
    Check(facts.Values.All(f => f.Left is >= FactBoard.LowestFactor and <= FactBoard.HighestFactor
                               && f.Right is >= FactBoard.LowestFactor and <= FactBoard.HighestFactor), "facts use factors 3 to 12");
    Check(s.LegalMoves().Count == 7 && s.LegalMoves().All(m => !m.IsJump), "Red has 7 opening steps");
    Check(s.TurnsUntilShuffle == 5, "first shuffle is 5 turns away");
    s.NewGame();
    Check(s.CurrentPlayer == Side.Black, "the other player starts the next game");
}

// ---- A plain move ----
{
    var s = NewSession();
    var from = new Square(5, 0);
    var to = new Square(4, 1);
    var oldFromFact = s.Facts[from];
    Check(Play(s, from, to) == AnswerResult.Correct, "right answer moves the piece");
    Check(s.Board[to] is Piece p && p.Side == Side.Red && s.Board[from] is null, "piece is on the new square");
    Check(!s.Facts[from].Key.Equals(oldFromFact.Key), "vacated square gets a different fact");
    Check(s.Facts.All.Values.Select(f => f.Key).Distinct().Count() == 32, "facts stay unique after a move");
    Check(s.CurrentPlayer == Side.Black && s.TurnsTaken == 1, "turn passes to Black");
}

// ---- Wrong answers ----
{
    var s = NewSession();
    Check(Play(s, new Square(5, 0), new Square(4, 1), answerRight: false) == AnswerResult.WrongTurnLost, "wrong answer loses the turn");
    Check(s.Board[new Square(5, 0)] is not null && s.Board[new Square(4, 1)] is null, "piece did not move");
    Check(s.CurrentPlayer == Side.Black && s.TurnsTaken == 1, "lost turn still counts as a turn");
    Check(s.Attempts[Side.Red] == 1 && s.CorrectAnswers[Side.Red] == 0, "attempt counted, not marked right");

    var t = NewSession();
    t.Rule = WrongAnswerRule.TryAgain;
    Check(Play(t, new Square(5, 0), new Square(4, 1), answerRight: false) == AnswerResult.WrongTryAgain, "keep-trying rule asks again");
    Check(t.CurrentPlayer == Side.Red && t.Pending is not null, "same player, same question");
    Check(t.SubmitAnswer(t.PendingFact!.Value.Product) == AnswerResult.Correct, "then the right answer moves");
}

// ---- Shuffling every 5 turns ----
{
    var s = NewSession();
    for (var turn = 0; turn < 4; turn++)
    {
        var mover = s.LegalMoves()[0];
        Play(s, mover.From, mover.To, answerRight: false);
    }

    Check(s.TurnsUntilShuffle == 1 && s.ShuffleCount == 0, "after 4 turns, 1 turn to the shuffle");
    var before = s.Board.EmptyDarkSquares().ToDictionary(sq => sq, sq => s.Facts[sq]);
    var last = s.LegalMoves()[0];
    Play(s, last.From, last.To, answerRight: false);
    Check(s.ShuffleCount == 1 && s.TurnsUntilShuffle == 5, "5th turn shuffles and restarts the countdown");
    Check(before.All(kv => !s.Facts[kv.Key].Key.Equals(kv.Value.Key)), "every open square got a different fact");
    Check(s.Facts.All.Values.Select(f => f.Key).Distinct().Count() == 32, "facts stay unique after a shuffle");
}

// ---- Captures and the must-jump rule ----
{
    // Red at (5,2) can jump Black at (4,3) to (3,4). Red at (5,6) could also step.
    var s = Position(Side.Red, (5, 2, "R"), (4, 3, "B"), (5, 6, "R"), (0, 1, "B"));
    var moves = s.LegalMoves();
    Check(moves.Count == 1 && moves[0].IsJump, "when you can jump, you must");
    Check(!s.Select(new Square(5, 6)), "pieces without a jump cannot be picked");

    s.MustJump = false;
    Check(s.LegalMoves().Any(m => !m.IsJump) && s.LegalMoves().Any(m => m.IsJump), "with jumps optional, steps are allowed too");
    s.MustJump = true;

    var capturedFact = s.Facts[new Square(4, 3)];
    Check(Play(s, new Square(5, 2), new Square(3, 4)) == AnswerResult.Correct, "jump answered right");
    Check(s.Board[new Square(4, 3)] is null && s.Board.Count(Side.Black) == 1, "jumped piece is captured");
    Check(!s.Facts[new Square(4, 3)].Key.Equals(capturedFact.Key), "captured square gets a different fact");
}

// ---- Multi-jump ----
{
    // Red at (6,1) jumps (5,2) to (4,3), then (3,4) to (2,5).
    var s = Position(Side.Red, (6, 1, "R"), (5, 2, "B"), (3, 4, "B"), (0, 7, "B"));
    Check(Play(s, new Square(6, 1), new Square(4, 3)) == AnswerResult.CorrectKeepJumping, "first jump says keep jumping");
    Check(s.CurrentPlayer == Side.Red && s.JumpingFrom == new Square(4, 3), "same piece must keep going");
    Check(!s.Select(new Square(4, 3)) && s.Selected == new Square(4, 3), "jumping piece stays picked");
    Check(Play(s, new Square(4, 3), new Square(2, 5)) == AnswerResult.Correct, "second jump answered right");
    Check(s.Board.Count(Side.Black) == 1 && s.CurrentPlayer == Side.Black, "both captured, then Black's turn");

    var w = Position(Side.Red, (6, 1, "R"), (5, 2, "B"), (3, 4, "B"), (0, 7, "B"));
    Play(w, new Square(6, 1), new Square(4, 3));
    Check(Play(w, new Square(4, 3), new Square(2, 5), answerRight: false) == AnswerResult.WrongTurnLost, "wrong answer mid-jump ends the turn");
    Check(w.Board[new Square(4, 3)] is not null && w.Board.Count(Side.Black) == 2, "first capture stands, second piece safe");
}

// ---- Kings ----
{
    var s = Position(Side.Red, (1, 2, "R"), (6, 7, "B"));
    Play(s, new Square(1, 2), new Square(0, 1));
    Check(s.Board[new Square(0, 1)] is Piece p && p.IsKing, "reaching the far row makes a king");
    Check(s.CurrentPlayer == Side.Black, "crowning ends the turn");

    var k = Position(Side.Red, (4, 3, "RK"), (0, 7, "B"));
    var targets = k.LegalMoves().Select(m => m.To).ToHashSet();
    Check(targets.SetEquals([new Square(3, 2), new Square(3, 4), new Square(5, 2), new Square(5, 4)]), "kings move both ways");
}

// ---- Winning ----
{
    var s = Position(Side.Red, (5, 2, "R"), (4, 3, "B"));
    Play(s, new Square(5, 2), new Square(3, 4));
    Check(s.Winner == Side.Red && s.Wins[Side.Red] == 1 && s.IsOver, "capturing the last piece wins");
    Check(s.LegalMoves().Count == 0, "no moves once the game is over");

    // Black at (7,0) cannot move down off the board; Red steps away and Black is stuck.
    var t = Position(Side.Red, (2, 3, "R"), (7, 0, "B"));
    Play(t, new Square(2, 3), new Square(1, 2));
    Check(t.Winner == Side.Red, "leaving the other side with no moves wins");
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
return failures == 0 ? 0 : 1;
