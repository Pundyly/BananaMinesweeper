namespace Minesweeper.Core;

public struct Tile
{
    public bool IsMined;
    public int MinesNearby;
    public bool IsOpen;
    public bool IsFlagged;
}