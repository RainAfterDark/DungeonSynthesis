using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public interface IPropagator
{
    bool Collapse(WaveGrid grid, IModel model, int cellId);
}
