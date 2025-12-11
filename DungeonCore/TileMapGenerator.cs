using DungeonCore.Heuristic;
using DungeonCore.Model;
using DungeonCore.Propagator;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore;

public class TileMapGenerator<TBase> (
    MappedGrid<TBase> inputGrid,
    IModel model, 
    IHeuristic heuristic, 
    IPropagator propagator, 
    int outWidth, int outHeight,
    int seed = 0)
    where TBase : notnull
{
    private readonly Random _random = new(seed);
    private readonly WaveGrid _grid = new(outWidth, outHeight);
    
    public void Initialize()
    {
        model.Initialize(inputGrid, _random);
        _grid.Initialize(model.StateCount);
    }
    
    public PropagationResult Step()
    {
        var cellId = heuristic.PickNextCell(_grid);
        if (cellId == -1) return PropagationResult.Collapsed;
        return propagator.Collapse(_grid, model, cellId) 
            ? PropagationResult.Collapsing 
            : PropagationResult.Contradicted;
    }

    public PropagationResult Generate()
    {
        while (true)
        {
            var result = Step();
            if (result != PropagationResult.Collapsing)
            {
                return result;
            }
        }
    }

    public TBase[] ToBase()
    {
        var cellStates = _grid.Cells.Select(c => c.Observed).ToArray();
        var cellIds = cellStates.Select(model.GetTileId).ToArray();
        return inputGrid.ToBase(cellIds, outWidth, outHeight);
    }
}
