using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Model;

public class CellState(int[] data, int n)
{
    private int[] Data { get; } = data;
    private int N { get; } = n;
    public int TileId { get; } = data[0];
    public double Weight { get; set; } = 1.0;
    private List<int>[] Neighbors { get; } = [new(), new(), new(), new()];
    public IReadOnlyList<int> GetNeighbors(int dir) => Neighbors[dir];

    private bool CanBeAdjacent(CellState neighbor, int dx, int dy)
    {
        var yMin = Math.Max(dy, 0);
        var yMax = Math.Min(dy + N, N);
        var xMin = Math.Max(dx, 0);
        var xMax = Math.Min(dx + N, N);

        for (var y = yMin; y < yMax; y++)
        {
            for (var x = xMin; x < xMax; x++)
            {
                if (Data[y * N + x] != neighbor.Data[(y - dy) * N + (x - dx)])
                    return false;
            }
        }
        return true;
    }

    public void TryAddNeighbor(int stateId, CellState state)
    {
        if (state.N != N) return;
        for (var i = 0; i < 4; i++)
        {
            if (CanBeAdjacent(state, Direction.Dx[i], Direction.Dy[i]))
                Neighbors[i].Add(stateId);
        }
    }

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(N);
        for (var y = 0; y < N; y++)
        {
            for (var x = 0; x < N; x++)
            {
                hc.Add(Data[y * N + x]);
            }
        }
        return hc.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not CellState other) return false;
        if (N != other.N) return false;
        for (var y = 0; y < N; y++)
            for (var x = 0; x < N; x++)
                if (Data[y * N + x] != other.Data[y * N + x]) return false;
        return true;
    }

    public override string ToString() => Helpers.GridToString(Data, N, N, 2);

    public string ToMappedString<TBase>(MappedGrid<TBase> mappedGrid, int width = 1) 
        where TBase : notnull
    {
        var baseGrid = mappedGrid.ToBase(Data, N, N);
        return Helpers.GridToString(baseGrid, N, N, width);
    }
}