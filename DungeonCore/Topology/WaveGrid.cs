using DungeonCore.Shared;

namespace DungeonCore.Topology;

public class WaveGrid
{
    private int Width { get; }
    private int Height { get; }
    public int CellCount { get; }
    public WaveCell[] Cells { get; }

    public event Action<int, int>? DomainReduced; // (cellId, removedState)
    public event Action<int, int>? Observed;      // (cellId, chosenState)

    public WaveGrid(int width, int height)
    {
        Width = width;
        Height = height;
        CellCount = Width * Height;
        Cells = new WaveCell[CellCount];
    }

    public void Initialize(int stateCount)
    {
        for (var i = 0; i < CellCount; i++)
            Cells[i] = new WaveCell(stateCount);
    }

    public int ToId(int x, int y) => y * Width + x;
    public (int x, int y) FromId(int id) => (id % Width, id / Width);

    public IEnumerable<(int neighborId, int dir)> NeighborsOf(int id)
    {
        var (x, y) = FromId(id);
        for (int dir = 0; dir < 4; dir++)
        {
            int nx = x + Direction.Dx[dir];
            int ny = y + Direction.Dy[dir];
            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                yield return (ToId(nx, ny), dir);
        }
    }

    public bool Observe(int cellId, int state)
    {
        var cell = Cells[cellId];
        if (cell.Observed != -1) return false;
        cell.SetObserved(state);
        Observed?.Invoke(cellId, state);
        return true;
    }

    public bool Ban(int cellId, int state)
    {
        var cell = Cells[cellId];
        var changed = cell.Ban(state);
        if (changed) DomainReduced?.Invoke(cellId, state);
        return changed;
    }
}