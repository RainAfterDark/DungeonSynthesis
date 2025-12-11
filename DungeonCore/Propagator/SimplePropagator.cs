using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public class SimplePropagator(int depth) : IPropagator
{
    public bool Observe(WaveGrid grid, IModel model, int cellId, int chosenState)
    {
        return grid.Observe(cellId, chosenState) && Propagate(grid, model);
    }

    public bool Propagate(WaveGrid grid, IModel model)
    {
        // In this simple propagator, we'll re-check all cells for propagation opportunities.
        // A more advanced propagator would use a queue of affected cells.
        for (var i = 0; i < depth; i++)
        {
            var changed = false;
            for (var cellId = 0; cellId < grid.CellCount; cellId++)
            {
                var cell = grid.Cells[cellId];
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
                        if (grid.Ban(neighborId, neighborState))
                        {
                            changed = true;
                        }
                    }
                }
            }
            if (!changed) break;
        }
        return true;
    }

    public bool Ban(WaveGrid grid, IModel model, int cellId, int state)
    {
        return grid.Ban(cellId, state);
    }
}
