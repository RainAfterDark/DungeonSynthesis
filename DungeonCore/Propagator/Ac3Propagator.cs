using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public class Ac3Propagator : IPropagator
{
    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        var state = model.PickState(grid.Cells[cellId]);
        if (state == -1 || !grid.Observe(cellId, state)) return false;
        Propagate(grid, model, cellId);
        return true;
    }

    private void Propagate(WaveGrid grid, IModel model, int initialCellId)
    {
        var dirtyStack = new Stack<int>();
        dirtyStack.Push(initialCellId);

        while (dirtyStack.Count > 0)
        {
            var cellId = dirtyStack.Pop();
            var cell = grid.Cells[cellId];

            foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
            {
                var nCell = grid.Cells[neighborId];
                var oppositeDir = Direction.Invert(dir);
                var changed = false;

                for (var nState = 0; nState < model.StateCount; nState++)
                {
                    if (!nCell.Domain[nState]) continue;
                    var isCompatible = model
                        .GetNeighbors(nState, oppositeDir)
                        .Any(allowedNeighbor => cell.Domain[allowedNeighbor]);

                    if (isCompatible) continue;
                    if (!grid.Ban(neighborId, nState)) continue; 
                    changed = true;
                }

                if (!changed) continue;
                dirtyStack.Push(neighborId);
            }
        }
    }
}
