using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public interface IHeuristic
{
    int PickNextCell(WaveGrid grid);
    void OnDomainReduced(int cellId, int removedState);
    void OnObserved(int cellId, int chosenState);
}
