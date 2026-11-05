# Connect 2×2

A Connect 4 game for practising times tables. The board is the classic 7 × 6 grid, with the columns numbered **6 to 12** along the bottom and the rows numbered **5 to 10** up the left side. To drop a piece, you have to answer the multiplication for the spot it would land on (row number × column number). Get it right and the piece drops in; first to four in a row wins.

It's a .NET 10 Blazor WebAssembly app: a single static page that runs entirely in the browser. The game logic, board rendering, animations, and keyboard/number-pad input are all written from scratch. The only packages are Microsoft's own Blazor runtime.

## Run it locally

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`dotnet --version` should print `10.x`).
2. From the repo root:

   ```sh
   dotnet run --project src/Connect2x2
   ```

3. Open **http://localhost:5222** (the browser usually opens on its own).

For live reload while editing, use `dotnet watch --project src/Connect2x2` instead.

## How to play

- Players take turns. Click a column (or use the arrow keys and Enter). The question is for the lowest open spot in that column.
- Type the answer with the keyboard or the on-screen number pad, then press Enter or ✓. Esc or "Pick a different column" backs out.
- **Wrong answer?** Choose under the board: *Lose your turn* (the answer is shown and play passes on) or *Keep trying*.
- Win by connecting four across, up, or diagonally. Scores and "facts right" carry over between games, and the starting player alternates.

## Play it online

Every push to `main` deploys the game to GitHub Pages at **https://ericleefriedman.github.io/Connect-2x2/** (one-time setup: repo Settings → Pages → Source: *GitHub Actions*). It works on phones, tablets, and computers.

## Putting it on another website

```sh
dotnet publish src/Connect2x2 -c Release -o publish
```

Everything in `publish/wwwroot` is plain static files (HTML, CSS, JS, and WebAssembly), so it can be uploaded to any static host: GitHub Pages, Netlify, Azure Static Web Apps, or a regular web server. If the site lives in a sub-folder (for example `https://example.com/connect/`), change `<base href="/" />` in `wwwroot/index.html` to that folder (`<base href="/connect/" />`).

## Project layout

```
src/Connect2x2/
  Game/Board.cs        grid, gravity, and four-in-a-row detection
  Game/GameSession.cs  turns, questions, wrong-answer rule, scores
  App.razor            the page: board, scoreboard, question card, input handling
  wwwroot/css/app.css  all styling and animations
  wwwroot/index.html   host page
```
