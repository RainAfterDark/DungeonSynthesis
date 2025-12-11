using DungeonCore.Topology;

namespace DungeonCore.Model;

public interface IModel
{
    int StateCount { get; }
    IReadOnlyList<int> GetNeighbors(int stateId, int dir);
    int GetTileId(int stateId);
    void Initialize<TBase>(MappedGrid<TBase> inputGrid, Random random) where TBase : notnull;
    int PickState(WaveCell cell);
}
