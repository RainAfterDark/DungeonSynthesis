using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public class RecursivePropagator(int maxDepth = int.MaxValue) : IPropagator
{
    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        var state = model.PickState(grid.Cells[cellId]);
        if (state == -1 || !grid.Observe(cellId, state)) return false;
        Propagate(grid, model, cellId, maxDepth);
        return true;
    }

    public int Propagate(WaveGrid grid, IModel model, int cellId, int depth)
    {
        if (depth == 0) return maxDepth;
        var cell = grid.Cells[cellId];
        var deepest = maxDepth - depth;
        foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
        {
            var neighbor = grid.Cells[neighborId];
            var oppositeDir = Direction.Invert(dir);

            for (var neighborState = 0; neighborState < model.StateCount; neighborState++)
            {
                if (!neighbor.Domain[neighborState]) continue;
                var isCompatible = model.GetNeighbors(neighborState, oppositeDir)
                    .Any(allowedNeighbor => cell.Domain[allowedNeighbor]);

                if (isCompatible) continue;
                if (!grid.Ban(neighborId, neighborState)) continue;
                var newDepth = Propagate(grid, model, neighborId, depth - 1);
                deepest = Math.Max(deepest, newDepth);
            }
        }
        return deepest;
    }
}
