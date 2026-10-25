/// Code made by Claude Code in 25/10/2026
/// Sowwy for this.
/// I just don't know how to use Raylib properly. I should study this actually.

using DiscordRPC;
using Minesweeper.Core;
using Raylib_cs;
using System.Numerics;

// ====== CONFIG ======
const string DiscordAppId = "1563924645068144643";
const int Cell = 45;
const int HeaderH = 56;
const int Pad = 10;
const int MenuW = 440, MenuH = 525;
// ====================

// ====== TILE ACCESS ======
static bool IsMine(Board b, int x, int y)    => b.Map[x, y].IsMined;
static bool IsOpen(Board b, int x, int y)    => b.Map[x, y].IsOpen;
static bool IsFlagged(Board b, int x, int y) => b.Map[x, y].IsFlagged;
// =========================

// ====== DISCORD RPC ======
DiscordRpcClient? discord = null;
try
{
    discord = new DiscordRpcClient(DiscordAppId);
    discord.Initialize();
}
catch { discord = null; } // game works fine without Discord

DateTime gameStartUtc = DateTime.UtcNow;
double lastPresenceUpdate = 0;

void SetPresence(string details, string state, bool withTimer)
{
    if (discord == null) return;
    try
    {
        discord.SetPresence(new RichPresence
        {
            Details = details,
            State = state,
            Timestamps = withTimer ? new Timestamps(gameStartUtc) : null,
            Assets = new Assets
            {
                LargeImageKey = "logo",
                LargeImageText = "Banana Minesweeper"
            }
        });
    }
    catch { }
}
// =========================

Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
Raylib.InitWindow(MenuW, MenuH, "Banana Minesweeper");
Image icon = Raylib.LoadImage("app.png");
Raylib.ImageFormat(ref icon, PixelFormat.UncompressedR8G8B8A8); // окно требует RGBA 8 бит
Raylib.SetWindowIcon(icon);
Raylib.UnloadImage(icon);
Raylib.SetTargetFPS(144);

var presets = new (string Name, int W, int H, int Mines)[]
{
    ("Beginner",     9,  9, 10),
    ("Intermediate", 16, 16, 40),
    ("Expert",       30, 16, 99),
};

int customW = 20, customH = 15, customM = 50;

bool inMenu = true;
Board? board = null;
int w = 0, h = 0, mines = 0;
bool started = false, won = false, lost = false;
double startTime = 0, finalTime = 0;
int flagsCount = 0;
(int x, int y)? explodedCell = null;

Color bg       = new Color(36, 39, 48, 255);
Color panel    = new Color(52, 56, 68, 255);
Color accent   = new Color(255, 205, 60, 255);
Color closedLt = new Color(120, 128, 150, 255);
Color closedDk = new Color(84, 90, 110, 255);
Color openCol  = new Color(30, 32, 40, 255);
Color[] numCols =
{
    Color.White,
    new Color(90, 170, 255, 255),
    new Color(90, 210, 120, 255),
    new Color(255, 100, 100, 255),
    new Color(170, 120, 255, 255),
    new Color(255, 160, 70, 255),
    new Color(70, 220, 220, 255),
    new Color(230, 230, 230, 255),
    new Color(160, 160, 160, 255),
};

SetPresence("In the main menu", "Choosing difficulty", false);

void StartGame(int gw, int gh, int gm)
{
    w = gw; h = gh;
    mines = Math.Clamp(gm, 1, gw * gh - 1);
    board = new Board();
    board.GenerateMap(w, h, mines);
    started = won = lost = false;
    flagsCount = 0;
    explodedCell = null;
    inMenu = false;
    gameStartUtc = DateTime.UtcNow;
    Raylib.SetWindowSize(Math.Max(w * Cell + Pad * 2, 300), h * Cell + HeaderH + Pad * 2);
    SetPresence($"Playing {w}x{h}", $"Mines left: {mines}", true);
    lastPresenceUpdate = Raylib.GetTime();
}

void ToMenu()
{
    inMenu = true;
    Raylib.SetWindowSize(MenuW, MenuH);
    SetPresence("In the main menu", "Choosing difficulty", false);
}

bool UiButton(Rectangle r, string text, int fontSize = 20, bool highlight = false)
{
    Vector2 m = Raylib.GetMousePosition();
    bool hover = Raylib.CheckCollisionPointRec(m, r);
    Color c = highlight ? accent : (hover ? new Color(80, 86, 105, 255) : panel);
    Raylib.DrawRectangleRounded(r, 0.25f, 8, c);
    Color tc = highlight ? Color.Black : Color.White;
    int tw = Raylib.MeasureText(text, fontSize);
    Raylib.DrawText(text, (int)(r.X + (r.Width - tw) / 2), (int)(r.Y + (r.Height - fontSize) / 2), fontSize, tc);
    return hover && Raylib.IsMouseButtonReleased(MouseButton.Left);
}

void Stepper(string label, ref int value, int min, int max, int y, int step = 1)
{
    Raylib.DrawText(label, 60, y + 8, 20, Color.White);
    if (UiButton(new Rectangle(230, y, 36, 36), "-", 24)) value = Math.Max(min, value - step);
    string s = value.ToString();
    int tw = Raylib.MeasureText(s, 22);
    Raylib.DrawText(s, 290 + (50 - tw) / 2, y + 7, 22, accent);
    if (UiButton(new Rectangle(350, y, 36, 36), "+", 24)) value = Math.Min(max, value + step);
}

void DrawMenu()
{
    string title = "MINESWEEPER";
    int tw = Raylib.MeasureText(title, 36);
    Raylib.DrawText(title, (MenuW - tw) / 2, 28, 36, accent);

    int y = 90;
    foreach (var p in presets)
    {
        if (UiButton(new Rectangle(60, y, 320, 50), $"{p.Name}  {p.W}x{p.H}, {p.Mines} mines", 20))
            StartGame(p.W, p.H, p.Mines);
        y += 62;
    }

    Raylib.DrawLine(60, y + 4, 380, y + 4, closedDk);
    Raylib.DrawText("Custom game", 60, y + 14, 18, closedLt);
    y += 44;

    Stepper("Width", ref customW, 5, 40, y); y += 44;
    Stepper("Height", ref customH, 5, 24, y); y += 44;
    customM = Math.Clamp(customM, 1, customW * customH - 1);
    Stepper("Mines", ref customM, 1, customW * customH - 1, y, 5);
    customM = Math.Clamp(customM, 1, customW * customH - 1);
    y += 52;

    if (UiButton(new Rectangle(60, y, 320, 44), "Play", 22, true))
        StartGame(customW, customH, customM);
}

void DrawFlag(int px, int py)
{
    Raylib.DrawRectangle(px + 14, py + 7, 3, 16, Color.White);
    Raylib.DrawTriangle(new Vector2(px + 15, py + 6), new Vector2(px + 15, py + 16),
                        new Vector2(px + 5, py + 11), new Color(255, 80, 80, 255));
    Raylib.DrawRectangle(px + 8, py + 22, 15, 3, Color.White);
}

void DrawMine(int px, int py, bool exploded)
{
    if (exploded) Raylib.DrawRectangle(px, py, Cell - 1, Cell - 1, new Color(180, 40, 40, 255));
    int cx = px + Cell / 2, cy = py + Cell / 2;
    Raylib.DrawCircle(cx, cy, 7, Color.Black);
    Raylib.DrawLine(cx - 11, cy, cx + 11, cy, Color.Black);
    Raylib.DrawLine(cx, cy - 11, cx, cy + 11, Color.Black);
    Raylib.DrawLine(cx - 8, cy - 8, cx + 8, cy + 8, Color.Black);
    Raylib.DrawLine(cx - 8, cy + 8, cx + 8, cy - 8, Color.Black);
    Raylib.DrawCircle(cx - 2, cy - 2, 2, Color.White);
}

bool CheckWin()
{
    int opened = 0;
    for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            if (IsOpen(board!, x, y)) opened++;
    if (opened == w * h - mines) return true;
    return board!.IsAllMinesFlagged() && flagsCount == mines;
}

void HandleGameInput()
{
    if (Raylib.IsKeyPressed(KeyboardKey.R)) { StartGame(w, h, mines); return; }
    if (Raylib.IsKeyPressed(KeyboardKey.Escape)) { ToMenu(); return; }

    int sw = Raylib.GetScreenWidth();
    int offX = (sw - w * Cell) / 2;
    int offY = HeaderH + Pad;

    Rectangle face = new Rectangle(sw / 2 - 20, 8, 40, 40);
    if (Raylib.IsMouseButtonReleased(MouseButton.Left) &&
        Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), face))
    {
        StartGame(w, h, mines);
        return;
    }

    Rectangle menuBtn = new Rectangle(sw - 84, 8, 70, 40);
    if (Raylib.IsMouseButtonReleased(MouseButton.Left) &&
        Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), menuBtn))
    {
        ToMenu();
        return;
    }

    if (won || lost) return;

    // Periodic Discord update (Discord rate-limits presence to ~1 per 15s)
    if (started && Raylib.GetTime() - lastPresenceUpdate > 15)
    {
        SetPresence($"Playing {w}x{h}", $"Mines left: {Math.Max(0, mines - flagsCount)}", true);
        lastPresenceUpdate = Raylib.GetTime();
    }

    Vector2 m = Raylib.GetMousePosition();
    int cx = (int)Math.Floor((m.X - offX) / Cell);
    int cy = (int)Math.Floor((m.Y - offY) / Cell);
    if (m.X < offX || m.Y < offY || cx < 0 || cy < 0 || cx >= w || cy >= h) return;

    if (Raylib.IsMouseButtonPressed(MouseButton.Left) && !IsFlagged(board!, cx, cy))
    {
        if (!started) { started = true; startTime = Raylib.GetTime(); }

        if (IsMine(board!, cx, cy))
        {
            lost = true;
            explodedCell = (cx, cy);
            finalTime = Raylib.GetTime() - startTime;
            SetPresence("Hit a mine", $"{w}x{h} - lasted {(int)finalTime}s", false);
            return;
        }
        if (!IsOpen(board!, cx, cy))
            board!.OpenTile(cx, cy);
    }
    else if (Raylib.IsMouseButtonPressed(MouseButton.Right) && !IsOpen(board!, cx, cy))
    {
        bool flagged = IsFlagged(board!, cx, cy);
        board!.SetFlag(cx, cy, !flagged);
        flagsCount += flagged ? -1 : 1;
        if (!started) { started = true; startTime = Raylib.GetTime(); }
    }

    if (!lost && CheckWin())
    {
        won = true;
        finalTime = Raylib.GetTime() - startTime;
        SetPresence("Won the game!", $"{w}x{h} cleared in {(int)finalTime}s", false);
    }
}

void DrawGame()
{
    int sw = Raylib.GetScreenWidth();
    int offX = (sw - w * Cell) / 2;
    int offY = HeaderH + Pad;

    // Header
    Raylib.DrawRectangle(0, 0, sw, HeaderH, panel);
    Raylib.DrawText($"Mines: {Math.Max(0, mines - flagsCount)}", 14, 8, 20, Color.White);

    double t = !started ? 0 : (won || lost ? finalTime : Raylib.GetTime() - startTime);
    Raylib.DrawText($"Time: {(int)t}s", 14, 32, 18, closedLt);

    string icon = lost ? "x_x" : won ? "B)" : ":)";
    UiButton(new Rectangle(sw / 2 - 20, 8, 40, 40), icon, 20, true);
    UiButton(new Rectangle(sw - 84, 8, 70, 40), "Menu", 18);

    // Grid
    Vector2 m = Raylib.GetMousePosition();
    for (int x = 0; x < w; x++)
    {
        for (int y = 0; y < h; y++)
        {
            int px = offX + x * Cell, py = offY + y * Cell;
            Rectangle r = new Rectangle(px, py, Cell - 1, Cell - 1);
            bool open = IsOpen(board!, x, y);
            bool flag = IsFlagged(board!, x, y);
            bool mine = IsMine(board!, x, y);

            if (lost && mine && !flag)
            {
                Raylib.DrawRectangle(px, py, Cell - 1, Cell - 1, openCol);
                DrawMine(px, py, explodedCell == (x, y));
            }
            else if (open)
            {
                Raylib.DrawRectangle(px, py, Cell - 1, Cell - 1, openCol);
                int n = board!.Map[x, y].MinesNearby;
                if (n > 0)
                {
                    string s = n.ToString();
                    int tw = Raylib.MeasureText(s, 20);
                    Raylib.DrawText(s, px + (Cell - tw) / 2, py + 5, 20, numCols[Math.Min(n, 8)]);
                }
            }
            else
            {
                bool hover = !won && !lost && Raylib.CheckCollisionPointRec(m, r);
                Raylib.DrawRectangle(px, py, Cell - 1, Cell - 1, hover ? closedLt : closedDk);
                Raylib.DrawRectangle(px, py, Cell - 1, 3, closedLt);
                Raylib.DrawRectangle(px, py, 3, Cell - 1, closedLt);
                if (flag)
                {
                    DrawFlag(px, py);
                    if (lost && !mine)
                        Raylib.DrawText("X", px + 9, py + 4, 22, new Color(255, 70, 70, 255));
                }
            }
        }
    }

    // End-of-game banner
    if (won || lost)
    {
        string msg = won ? "You win! R - restart, Esc - menu" : "Boom! R - restart, Esc - menu";
        int tw = Raylib.MeasureText(msg, 20);
        int bx = (sw - tw) / 2 - 12, by = Raylib.GetScreenHeight() / 2 - 20;
        Raylib.DrawRectangleRounded(new Rectangle(bx, by, tw + 24, 40), 0.3f, 8, new Color(0, 0, 0, 200));
        Raylib.DrawText(msg, bx + 12, by + 10, 20,
            won ? new Color(90, 220, 120, 255) : new Color(255, 100, 100, 255));
    }
}

while (!Raylib.WindowShouldClose())
{
    if (!inMenu) HandleGameInput();

    Raylib.BeginDrawing();
    Raylib.ClearBackground(bg);
    if (inMenu) DrawMenu(); else DrawGame();
    Raylib.EndDrawing();
}

discord?.ClearPresence();
discord?.Dispose();
Raylib.CloseWindow();