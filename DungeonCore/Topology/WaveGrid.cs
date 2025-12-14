using System.Runtime.CompilerServices;
using DungeonCore.Heuristic;
using DungeonCore.Model;
using DungeonCore.Shared.Data;

namespace DungeonCore.Topology;

public class WaveGrid
{
    private int Width { get; }
    private int Height { get; }
    public int CellCount { get; }
    public WaveCell[] Cells { get; }

    public event Action<int, int>? Banned; // (cellId, removedState)
    public event Action<int, int>? Observed; // (cellId, chosenState)

    public WaveGrid(int width, int height)
    {
        Width = width;
        Height = height;
        CellCount = Width * Height;
        Cells = new WaveCell[CellCount];
    }

    public void Initialize(IModel model, IHeuristic heuristic)
    {
        for (var i = 0; i < CellCount; i++)
            Cells[i] = new WaveCell(model.StateCount, model.SumWeights);
        
        // Subscribe events
        Banned = heuristic.OnBanned;
        Observed = heuristic.OnObserved;
        
        // Apply boundary constraints (for non-periodic models)
        // We define the valid coordinate ranges for every state.
        var xMin = new int[model.StateCount];
        var xMax = new int[model.StateCount];
        var yMin = new int[model.StateCount];
        var yMax = new int[model.StateCount];

        for (var i = 0; i < model.StateCount; i++)
        {
            // Default: A state is valid everywhere (0 to Width-1, 0 to Height-1).
            xMin[i] = 0; xMax[i] = Width - 1;
            yMin[i] = 0; yMax[i] = Height - 1;

            // Check UP (Dir 0): If no neighbors, it must be very Top
            if (!model.HasSupport(i, 0)) yMax[i] = 0; 
            // Check RIGHT (Dir 1): If no neighbors, must be at very Right
            if (!model.HasSupport(i, 1)) xMin[i] = Width - 1;
            // Check DOWN (Dir 2): If no neighbors, must be at very Bottom
            if (!model.HasSupport(i, 2)) yMin[i] = Height - 1;
            // Check LEFT (Dir 3): If no neighbors, must be at very Left
            if (!model.HasSupport(i, 3)) xMax[i] = 0;
        }
        
        // Go through each state in each cell
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                for (var s = 0; s < model.StateCount; s++)
                {
                    if (x >= xMin[s] && x <= xMax[s] && y >= yMin[s] && y <= yMax[s]) continue;
                    // Ban if state cannot be within bounds
                    Cells[ToId(x, y)].Ban(s, model.GetWeight(s));
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ToId(int x, int y) => y * Width + x;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int x, int y) FromId(int id) => (id % Width, id / Width);
    
    public IEnumerable<(int neighborId, int dir)> NeighborsOf(int id)
    {
        var (x, y) = FromId(id);
        for (var dir = 0; dir < 4; dir++)
        {
            var nx = x + Direction.Dx[dir];
            var ny = y + Direction.Dy[dir];
            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                yield return (ToId(nx, ny), dir);
        }
    }

    public bool Observe(int cellId, int stateId)
    {
        var cell = Cells[cellId];
        if (cell.Observed != -1) return false;
        cell.Observe(stateId);
        Observed?.Invoke(cellId, stateId);
        return true;
    }

    public bool Ban(int cellId, int stateId, double weight)
    {
        var cell = Cells[cellId];
        var changed = cell.Ban(stateId, weight);
        if (changed) Banned?.Invoke(cellId, stateId);
        return changed;
    }
}