using DungeonCore.Shared;

namespace DungeonCore.Topology;

public class WaveGrid
{
    private int Width { get; }
    private int Height { get; }
    public int CellCount { get; }
    public WaveCell[] Cells { get; }

    public event Action<int, int>? Banned; // (cellId, removedState)
    public event Action<int, int>? Observed;      // (cellId, chosenState)

    public WaveGrid(int width, int height)
    {
        Width = width;
        Height = height;
        CellCount = Width * Height;
        Cells = new WaveCell[CellCount];
    }

    public void Initialize(int stateCount, double sumWeights)
    {
        for (var i = 0; i < CellCount; i++)
            Cells[i] = new WaveCell(stateCount, sumWeights);
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