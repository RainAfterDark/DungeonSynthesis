using System.Text;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Model;

public class OverlappingModel(int n, bool periodic = true, bool symmetrical = false) : IModel
{
    private Random _random = new();
    
    private List<CellState> States { get; } = new();
    public int StateCount { get; private set; }
    public int GetTileId(int stateId) => stateId == -1 ? -1 : States[stateId].TileId;
    public IReadOnlyList<int> GetNeighbors(int stateId, int dir) => States[stateId].GetNeighbors(dir);

    public void Initialize<TBase>(MappedGrid<TBase> inputGrid, Random random) where TBase : notnull
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 1);
        var width = inputGrid.Width;
        var height = inputGrid.Height;
        var grid = inputGrid.ToTileIds();
        if (!periodic && (height < n || width < n)) 
            throw new ArgumentOutOfRangeException(nameof(inputGrid));

        _random = random;
        var yMax = periodic ? height : height - n + 1;
        var xMax = periodic ? width : width - n + 1;
        var map = new Dictionary<CellState, CellState>();

        // Look at the entire input grid
        for (var row = 0; row < yMax; row++)
        {
            for (var col = 0; col < xMax; col++)
            {
                // Create NxN pattern (state) in flat form
                var state = new int[n * n];
                for (var sy = 0; sy < n; sy++)
                {
                    for (var sx = 0; sx < n; sx++)
                    {
                        var gy = row + sy;
                        var gx = col + sx;
                        if (periodic)
                        {
                            gy %= height;
                            gx %= width;
                        }
                        state[sy * n + sx] = grid[gy * width + gx];
                    }
                }

                // Create symmetries if enabled
                var variants = symmetrical
                    ? Helpers.GetD4SymmetriesSquare(state, n)
                    : [state];
                foreach (var variant in variants)
                {
                    // Add probed patterns to map
                    var probe = new CellState(variant, n);
                    if (map.TryGetValue(probe, out var existing))
                        existing.Weight += 1.0;
                    else
                        map[probe] = probe;
                }
            }
        }

        // Add all states
        foreach (var kv in map)
            States.Add(kv.Value);
        StateCount = States.Count;

        // Build adjacency map
        foreach (var t0 in States)
        {
            var idx = 0;
            foreach (var t1 in States)
            {
                t0.TryAddNeighbor(idx++, t1);
            }
        }
    }
    
    public int PickState(WaveCell cell)
    {
        var sumOfWeights = 0.0;
        for (var i = 0; i < cell.Domain.Length; i++)
        {
            if (cell.Domain[i])
            {
                sumOfWeights += States[i].Weight;
            }
        }

        var r = _random.NextDouble() * sumOfWeights;
        for (var i = 0; i < cell.Domain.Length; i++)
        {
            if (!cell.Domain[i]) continue;
            r -= States[i].Weight;
            if (r < 0)
            {
                return i;
            }
        }

        return -1;
    }

    public string ToMappedString<TBase>(MappedGrid<TBase> mappedGrid)
        where TBase : notnull
    {
        var sb = new StringBuilder();
        var idx = 0;
        foreach (var st in States)
        {
            sb.AppendLine($"#{idx++} (w: {st.Weight})");
            sb.AppendLine(st.ToMappedString(mappedGrid));
            sb.AppendLine();
        }
        return sb.ToString();
    }
}