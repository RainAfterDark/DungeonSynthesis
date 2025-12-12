using DungeonCore.Topology;

namespace DungeonCore.Model;

public interface IModel
{
    int StateCount { get; }
    double SumWeights { get; }
    IReadOnlyList<int> GetNeighbors(int stateId, int dir);
    int GetTileId(int stateId);
    double GetWeight(int stateId);
    void Initialize<TBase>(MappedGrid<TBase> inputGrid, Random random) where TBase : notnull;
    int PickState(WaveCell cell);
}
