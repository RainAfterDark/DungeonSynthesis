using System.Text;

namespace DungeonCore.Shared;

public static class Helpers
{
    // --------------- Flat grid helpers ---------------

    public static string GridToString<T>(T[] data, int width, int height, int cellWidth = 1)
    {
        var sb = new StringBuilder();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var d = data[y * width + x];
                var s = d != null ? d.ToString() : "?";
                sb.Append(s!.PadRight(cellWidth));
            }
            if (y < height - 1) sb.AppendLine();
        }
        return sb.ToString();
    }

    public static (char[] data, int width, int height) StringToCharGrid(string grid, char fillChar = ' ')
    {
        var rows = grid.Split('\n');
        var height = rows.Length;
        var width = rows.Max(s => s.Length);
        var data = new char[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var row = rows[y];
                data[y * width + x] = x < row.Length ? row[x] : fillChar;
            }
        }
        return (data, width, height);
    }

    // --------------- Square matrix transforms on flat arrays (Dihedral D4) ---------------

    public static T[] CloneMatrix<T>(T[] src)
    {
        var dst = new T[src.Length];
        Array.Copy(src, dst, src.Length);
        return dst;
    }

    public static T[] FlipHorizontal<T>(T[] src, int n)
    {
        var dst = new T[n * n];
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                dst[y * n + x] = src[y * n + (n - 1 - x)];
        return dst;
    }

    public static T[] FlipVertical<T>(T[] src, int n)
    {
        var dst = new T[n * n];
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                dst[y * n + x] = src[(n - 1 - y) * n + x];
        return dst;
    }

    public static T[] Rotate90CW<T>(T[] src, int n)
    {
        var dst = new T[n * n];
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                dst[x * n + (n - 1 - y)] = src[y * n + x];
        return dst;
    }

    public static T[] Rotate180<T>(T[] src, int n) => Rotate90CW(Rotate90CW(src, n), n);

    public static T[] Rotate270CW<T>(T[] src, int n) => Rotate90CW(Rotate180(src, n), n);

    // Returns the 8 symmetries of a square (Dihedral group D4) for a flat n x n matrix
    // Sequence: R0, R90, R180, R270, then FlipH of each rotation.
    public static IEnumerable<T[]> GetD4SymmetriesSquare<T>(T[] src, int n)
    {
        var r0 = CloneMatrix(src);
        var r1 = Rotate90CW(r0, n);
        var r2 = Rotate90CW(r1, n);
        var r3 = Rotate90CW(r2, n);

        yield return r0;
        yield return r1;
        yield return r2;
        yield return r3;

        yield return FlipHorizontal(r0, n);
        yield return FlipHorizontal(r1, n);
        yield return FlipHorizontal(r2, n);
        yield return FlipHorizontal(r3, n);
    }
}