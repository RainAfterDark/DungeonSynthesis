using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public interface IPropagator
{
    // Set a cell to a chosen state and trigger propagation. Returns false on contradiction.
    bool Observe(WaveGrid grid, IModel model, int cellId, int chosenState);
    // Apply pending implications/queue processing. Returns false on contradiction.
    bool Propagate(WaveGrid grid, IModel model);
    // Ban a specific state from a cell. Returns false on contradiction.
    bool Ban(WaveGrid grid, IModel model, int cellId, int state);
}
