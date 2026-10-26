using System.Linq;

namespace Minesweeper.Core;

public class Board
{
    public Tile[,] Map { get; set; } = null!;
    public int Mines { get; set; }

    private bool IsFirstOpen;
    private int MinesQuantity;

    public void GenerateMap(int x, int y, int minesQuantity)
    {
        IsFirstOpen = true;
        Map = new Tile[x, y];
        MinesQuantity = minesQuantity;
    }

    private void PlaceMines(int cx, int cy)
    {
        int x = Map.GetLength(0);
        int y = Map.GetLength(1);
        Random rnd = new Random();
        for (int i = 0; i < MinesQuantity; i++)
        {
            int xb = rnd.Next(x);
            int yb = rnd.Next(y);
            if (xb >= cx - 1 && xb <= cx + 1 && yb >= cy - 1 && yb <= cy + 1)
                continue;

            Map[xb, yb].IsMined = true;
            Mines++;
        }
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                if (Map[i, j].IsMined)
                    continue;

                Map[i, j].MinesNearby = CheckMinesNearby(i, j);
            }
        }
    }

    public void SetBomb(int x, int y) => Map[x, y].IsMined = true;

    public void SetFlag(int x, int y, bool isSet = true)
    {
        if (!Map[x, y].IsOpen)
            Map[x, y].IsFlagged = isSet;
    }

    public int CheckMinesNearby(int x, int y)
    {
        int result = 0;
        for (int j = 0; j < 3; j++)
        {
            for (int p = 0; p < 3; p++)
            {
                if (x - 1 + j >= Map.GetLength(0) || x - 1 + j < 0 || y - 1 + p >= Map.GetLength(1) || y - 1 + p < 0)
                    continue;

                if (Map[x - 1 + j, y - 1 + p].IsMined)
                    result++;
            }
        }

        return result;
    }

    public void OpenTile(int x, int y)
    {
        if (IsFirstOpen)
        {
            PlaceMines(x, y);
            IsFirstOpen = false;
        }

        if (x < 0 || y < 0 || x >= Map.GetLength(0) || y >= Map.GetLength(1))
            return;

        if (Map[x, y].IsFlagged || Map[x, y].IsOpen)
            return;

        Map[x, y].IsOpen = true;

        if (Map[x, y].MinesNearby > 0 || Map[x, y].IsMined)
            return;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                OpenTile(x + dx, y + dy);
    }

    public bool IsAllMinesFlagged()
    {
        if ( Mines <= 0) return false;
        return Map.Cast<Tile>().Where(x => x.IsMined).All(x => x.IsFlagged);
    } 
}