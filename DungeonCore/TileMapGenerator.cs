using DungeonCore.Heuristic;
using DungeonCore.Model;
using DungeonCore.Propagator;
using DungeonCore.Shared.Data;
using DungeonCore.Shared.Util;
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
    private Random _random = new(seed);
    private readonly WaveGrid _grid = new(outWidth, outHeight);
    private readonly ConsoleRenderer _renderer = new(0, 1);
    private bool _initialized;
    
    public void Initialize()
    {
        model.Initialize(inputGrid, _random);
        _grid.Initialize(model, heuristic);
        propagator.Initialize(_grid, model);
        heuristic.Initialize(_grid, model, _random);
        
        _initialized = true;
    }
    
    public PropagationResult Step()
    {
        if (!_initialized) Initialize();
        
        var cellId = heuristic.PickNextCell(_grid);
        if (cellId == -1)
        {
            _initialized = false;
            return PropagationResult.Collapsed;
        }
        
        if (propagator.Collapse(_grid, model, cellId))
            return PropagationResult.Collapsing;
        
        _initialized = false;
        return PropagationResult.Contradicted;
    }

    public PropagationResult Generate(bool logProgress = false)
    {
        Initialize();
        while (true)
        {
            var result = Step();
            if (logProgress) WriteToConsole();
            if (result == PropagationResult.Collapsing) continue;
            return result;
        }
    }

    public void GenerateUntilCollapsed(bool logProgress = false)
    {
        var result = PropagationResult.Collapsing;
        while (result !=  PropagationResult.Collapsed)
        {
            _random = new Random();
            Initialize();
            while (true)
            {
                result = Step();
                if (logProgress) WriteToConsole();
                if (result == PropagationResult.Collapsing) continue;
                break;
            }
        }
    }

    public TBase[] ToBase()
    {
        var cellStates = _grid.Cells.Select(c => c.Observed).ToArray();
        var cellIds = cellStates.Select(model.GetTileId).ToArray();
        return inputGrid.ToBase(cellIds, outWidth, outHeight);
    }

    public override string ToString() => Helpers.GridToString(ToBase(), outWidth, outHeight);
    
    private void WriteToConsole()
    {
        _renderer.Render(ToString());
        Console.SetCursorPosition(0, outHeight + 1);
    }
}
