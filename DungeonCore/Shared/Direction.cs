namespace DungeonCore.Shared;

internal static class Direction
{
    public static readonly int[] Dx = [-1, 0, 1, 0];
    public static readonly int[] Dy = [0, 1, 0, -1];
    public static int Invert(int dir) => (dir + 2) % 4;
}