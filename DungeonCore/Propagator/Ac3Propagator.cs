using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public class Ac3Propagator : IPropagator
{
    private readonly Stack<int> _dirtyStack = new();
    
    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        var state = model.PickState(grid.Cells[cellId]);
        if (state == -1 || !grid.Observe(cellId, state)) return false;
        _dirtyStack.Push(cellId);
        return Propagate(grid, model);
    }

    private bool Propagate(WaveGrid grid, IModel model)
    {
        while (_dirtyStack.Count > 0)
        {
            var cellId = _dirtyStack.Pop();
            var cell = grid.Cells[cellId];
            if (cell.DomainCount < 1) return false;

            foreach (var (neighborId, dir) in grid.NeighborsOf(cellId))
            {
                var nCell = grid.Cells[neighborId];
                var oppositeDir = Direction.Invert(dir);
                var changed = false;

                foreach (var nState in nCell.GetPossibleStates())
                {
                    var isCompatible = model
                        .GetNeighbors(nState, oppositeDir)
                        .Any(support => cell.IsPossibleState(support));

                    if (isCompatible) continue;
                    var weight = model.GetWeight(nState);
                    if (!grid.Ban(neighborId, nState, weight)) continue; 
                    changed = true;
                }

                if (!changed) continue;
                _dirtyStack.Push(neighborId);
            }
        }

        return true;
    }
}
