using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public sealed class ScanlineHeuristic : IHeuristic
{
    private int _idx;

    public int PickNextCell(WaveGrid grid)
    {
        while (true)
        {
            if (_idx >= grid.CellCount) return -1;
            var cell = grid.Cells[_idx];
            _idx++;
            if (cell.Observed != -1) continue;
            return _idx - 1;
        }
    }
}
