using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public sealed class ScanlineHeuristic : IHeuristic
{
    public int PickNextCell(WaveGrid grid)
    {
        for (var id = 0; id < grid.CellCount; id++)
        {
            var cell = grid.Cells[id];
            if (cell.IsDecided) continue;
            return id;
        }
        return -1;
    }

    public void OnDomainReduced(int cellId, int removedState) { /* no-op */ }
    
    public void OnObserved(int cellId, int chosenState) { /* no-op */ }
}
