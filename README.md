# Math Games

Four classic board games for practising times tables, each at its own link:

- **Connect 2×2**: https://ericleefriedman.github.io/Connect-2x2/connect2x2/
- **Times-Table Checkers**: https://ericleefriedman.github.io/Connect-2x2/checkers/
- **Times-Table Tic-Tac-Toe**: https://ericleefriedman.github.io/Connect-2x2/tictactoe/
- **Times-Table Snakes & Ladders**: https://ericleefriedman.github.io/Connect-2x2/snakes/

The site root (https://ericleefriedman.github.io/Connect-2x2/) is a small landing page that links to all four. The links follow the repository name, so if the repository is renamed (for example to `math-games`) the games move to `https://ericleefriedman.github.io/math-games/connect2x2/` and `.../checkers/`, `.../tictactoe/` and `.../snakes/` on the next deploy, with no code changes.

## Connect 2×2

A Connect 4 game for practising times tables. The board is the classic 7 × 6 grid, with the columns numbered **6 to 12** along the bottom and the rows numbered **5 to 10** up the left side. To drop a piece, you have to answer the multiplication for the spot it would land on (row number × column number). Get it right and the piece drops in; first to four in a row wins.

It's a .NET 10 Blazor WebAssembly app: a single static page that runs entirely in the browser. The game logic, board rendering, animations, and keyboard/number-pad input are all written from scratch. The only packages are Microsoft's own Blazor runtime.

## Times-Table Checkers

Standard 8 × 8 checkers for two players on one device. Every dark square shows a multiplication fact (factors 3 to 12) right inside the square, and to move a piece onto a square you answer that square's fact.

- Tap a piece (the glowing ones can move), then tap a highlighted square, then answer its fact.
- A wrong answer loses the turn or lets you try again, the same setting as Connect 2×2.
- Jumps capture. Each landing in a multi-jump asks its own fact. Jumps are compulsory by default (there is a checkbox to make them optional).
- Reaching the far row makes a king, which moves both ways.
- No two squares show the same fact at once. A square that is left empty (moved off or captured) gets a new fact, never the one it had, and **every 5 turns all the open squares get new facts**. A meter above the board counts down.
- Win by capturing every enemy piece or leaving them no moves. 50 turns in a row with no capture and no new king is a draw.

## Times-Table Tic-Tac-Toe

Tic-tac-toe for two players on one device. Every square shows a multiplication fact, and you claim a square by answering it.

- Tap an open square, answer its fact, and the square is yours. A wrong answer loses the turn or lets you try again (the same setting as the other games), and a missed square gets a new fact, since the answer was just shown.
- **3 × 3** (three in a row) is the default. **4 × 4** needs four in a row.
- **Facts up to** 5 × 5, 10 × 10, or 12 × 12 (factors start at 2), so younger kids can play with easier facts.
- No two squares share a fact (on 4 × 4 with facts up to 5 there aren't enough to go round, so a few repeat).
- Scores, "facts right", and the starting player carry over between games like the others. Changing the board or the facts starts a new game.

## Times-Table Snakes & Ladders

Snakes and ladders for 2 to 4 players on one device, with the usual rules. The 100 squares each show a multiplication fact, and before you move you answer the fact on the square you'd land on.

- Tap **Roll**. The square you'd land on lights up and its fact pops up. Get it right and your token hops there.
- Land on the foot of a ladder and you climb it; land on a snake's head and you slide down to its tail.
- A wrong answer means you stay where you are and the turn passes, or you try again (the same setting as the other games). A missed square gets a new fact.
- You need the exact roll to reach 100; roll too high and you stay put. First to 100 wins.
- **Facts up to** 5 × 5, 10 × 10, or 12 × 12. Squares within one roll of each other never show the same fact.
- Scores, "facts right", and the starting player carry over between games. Changing the players or the facts starts a new game.

## Run it locally

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`dotnet --version` should print `10.x`).
2. From the repo root:

   ```sh
   dotnet run --project src/Connect2x2   # http://localhost:5222
   dotnet run --project src/Checkers     # http://localhost:5223
   dotnet run --project src/TicTacToe    # http://localhost:5224
   dotnet run --project src/SnakesLadders # http://localhost:5225
   ```

3. The browser usually opens on its own.

For live reload while editing, use `dotnet watch --project src/Checkers` (or `src/Connect2x2`) instead.

The checkers, tic-tac-toe, and snakes and ladders rules each have a small self-checking program (no test framework): `dotnet run --project tests/Checkers.Tests`, `dotnet run --project tests/TicTacToe.Tests`, and `dotnet run --project tests/SnakesLadders.Tests`. CI runs them on every build.

## How to play Connect 2×2

- Players take turns. Only the lowest empty spot in each column can be played; those spots glow in the current player's color and show their multiplication. Tap one (or use the arrow keys and Enter). Tapping anywhere else explains why and points to the spot you can play in that column.
- Type the answer with the keyboard or the on-screen number pad, then press Enter or ✓. Esc or "Pick a different column" backs out.
- **Wrong answer?** Choose under the board: *Lose your turn* (the answer is shown and play passes on) or *Keep trying*.
- Win by connecting four across, up, or diagonally. Scores and "facts right" carry over between games, and the starting player alternates.

## How changes ship

- **Pull requests merge themselves.** When the Build check passes on a pull request from a branch in this repo, the Auto-merge workflow merges it. Draft pull requests and pull requests from forks are never auto-merged.
- **Every pull request build keeps a copy of the site** as a `site-preview` artifact on the Actions run, for trying changes before they merge.
- **The site deploys only after main builds.** Every build of `main` that passes is deployed to GitHub Pages; a failing build leaves the current site in place.

## Putting it on another website

```sh
dotnet publish src/Connect2x2 -c Release -o publish/connect2x2
dotnet publish src/Checkers -c Release -o publish/checkers
dotnet publish src/TicTacToe -c Release -o publish/tictactoe
dotnet publish src/SnakesLadders -c Release -o publish/snakes
```

Everything in each `publish/<game>/wwwroot` is plain static files (HTML, CSS, JS, and WebAssembly), so it can be uploaded to any static host: GitHub Pages, Netlify, Azure Static Web Apps, or a regular web server. If the site lives in a sub-folder (for example `https://example.com/connect/`), change `<base href="/" />` in `wwwroot/index.html` to that folder (`<base href="/connect/" />`).

## Project layout

```
site/index.html          landing page linking to the games
src/Connect2x2/
  Game/Board.cs          grid, gravity, and four-in-a-row detection
  Game/GameSession.cs    turns, questions, wrong-answer rule, scores
  App.razor              the page: board, scoreboard, question card, input handling
  wwwroot/css/app.css    all styling and animations
  wwwroot/index.html     host page
src/Checkers/
  Game/Board.cs          8 x 8 board, steps, jumps, kings
  Game/Facts.cs          the fact in each dark square, replacing and shuffling
  Game/GameSession.cs    turns, multi-jumps, must-jump rule, the 5-turn shuffle, scores
  App.razor              the page
  wwwroot/css/app.css    all styling and animations
src/TicTacToe/
  Game/Board.cs          3 x 3 or 4 x 4 grid and line detection
  Game/Facts.cs          the fact in each square
  Game/GameSession.cs    turns, questions, wrong-answer rule, board size, fact range, scores
  App.razor              the page
  wwwroot/css/app.css    all styling and animations
src/SnakesLadders/
  Game/Board.cs          the 1 to 100 layout and where the snakes and ladders go
  Game/Facts.cs          the fact in each square
  Game/GameSession.cs    rolls, questions, wrong-answer rule, players, scores
  App.razor              the page: die, board, snakes and ladders drawing, tokens
  wwwroot/css/app.css    all styling and animations
tests/Checkers.Tests/    rule checks for the checkers game
tests/TicTacToe.Tests/   rule checks for the tic-tac-toe game
tests/SnakesLadders.Tests/ rule checks for the snakes and ladders game
```
