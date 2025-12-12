using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public interface IHeuristic
{
    void Initialize(WaveGrid grid, IModel model, Random random) {}
    int PickNextCell(WaveGrid grid);
    void OnBanned(int cellId, int stateId) {}
    void OnObserved(int cellId, int stateId) {}
}
