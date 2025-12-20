namespace DungeonCore.Topology;

public class MappedGrid<TBase> where TBase : notnull
{
    private readonly Dictionary<TBase, int> _base2Id = new();
    private readonly Dictionary<int, TBase> _id2Base = new();

    private TBase[] BaseGrid { get; }
    public int Width { get; }
    public int Height { get; }

    public MappedGrid(TBase[] grid, int width, int height, TBase unknownValue)
    {
        BaseGrid = grid;
        Width = width;
        Height = height;
        var id = 0;
        foreach (var cell in BaseGrid)
        {
            if (!_base2Id.TryAdd(cell, id)) continue;
            _id2Base.Add(id, cell);
            id++;
        }
        _base2Id.Add(unknownValue, -1);
        _id2Base.Add(-1, unknownValue);
    }

    public static MappedGrid<TBase> FromMapData(MapData<TBase> data) =>
        new(data.Grid, data.Width, data.Height, data.UnknownValue);

    public MapData<TBase> ToMapData() => 
        new(BaseGrid, Width, Height, _id2Base!.GetValueOrDefault(-1, default)!);

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

    public int[] ToTileIds() => ToTileIds(BaseGrid, Width, Height);

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