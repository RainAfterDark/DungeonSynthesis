using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public sealed class RecursivePropagator(int maxDepth = int.MaxValue) : IPropagator
{
    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        var state = model.PickState(grid.Cells[cellId]);
        if (state == -1 || !grid.Observe(cellId, state)) return false;
        return Propagate(grid, model, cellId, maxDepth);
    }

    private bool Propagate(WaveGrid grid, IModel model, int cellId, int depth)
    {
        var cell = grid.Cells[cellId];
        var valid = cell.DomainCount > 0;
        if (depth <= 0 || !valid) return valid;
        
        foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
        {
            var nCell = grid.Cells[neighborId];
            var oppositeDir = Direction.Invert(dir);

            for (var nState = 0; nState < model.StateCount; nState++)
            {
                if (!nCell.Domain[nState]) continue;
                var isCompatible = model
                    .GetNeighbors(nState, oppositeDir)
                    .Any(support => cell.Domain[support]);

                if (isCompatible) continue;
                var weight = model.GetWeight(nState);
                if (!grid.Ban(neighborId, nState, weight)) continue;
                if (Propagate(grid, model, neighborId, depth - 1)) continue;
                return false;
            }
        }
        return true;
    }
}
