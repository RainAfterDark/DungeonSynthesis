namespace DungeonCore.Topology;

public class MappedGrid<TBase> where TBase : notnull
{
    private readonly Dictionary<TBase, int> _base2Id = new();
    private readonly Dictionary<int, TBase> _id2Base = new();

    public TBase[] Base { get; }
    public int Width { get; }
    public int Height { get; }

    public MappedGrid(TBase[] data, int width, int height)
    {
        Base = data;
        Width = width;
        Height = height;
        var id = 0;
        foreach (var cell in Base)
        {
            if (_base2Id.TryAdd(cell, id) && _id2Base.TryAdd(id, cell))
            {
                id++;
            }
        }
    }

    // Backward-compat: allow constructing from 2D and flatten internally
    public MappedGrid(TBase[,] grid)
    {
        Height = grid.GetLength(0);
        Width = grid.GetLength(1);
        Base = new TBase[Width * Height];
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                Base[y * Width + x] = grid[y, x];

        var id = 0;
        foreach (var cell in Base)
        {
            if (_base2Id.TryAdd(cell, id) && _id2Base.TryAdd(id, cell))
            {
                id++;
            }
        }
    }

    public int[] ToTileIds(TBase[] data, int width, int height)
    {
        var newGrid = new int[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = y * width + x;
                newGrid[idx] = _base2Id.GetValueOrDefault(data[idx], -1);
            }
        }
        return newGrid;
    }

    public int[] ToTileIds()
    {
        return ToTileIds(Base, Width, Height);
    }

    public TBase[] ToBase(int[] data, int width, int height)
    {
        var newGrid = new TBase[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = y * width + x;
                if (_id2Base.TryGetValue(data[idx], out var t))
                    newGrid[idx] = t;
            }
        }
        return newGrid;
    }
}