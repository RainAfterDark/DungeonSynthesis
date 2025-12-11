using DungeonCore;
using DungeonCore.Heuristic;
using DungeonCore.Shared;
using DungeonCore.Topology;
using DungeonCore.Model;
using DungeonCore.Propagator;

var s =
    """
                
     ┌─────┐     
     │     └──┐ 
     │   WFC  │ 
     └──┐     │ 
        └─────┘ 
                
    """;
var (charData, width, height) = Helpers.StringToCharGrid(s);
var mg = new MappedGrid<char>(charData, width, height);
var baseFromIds = mg.ToBase(mg.ToTileIds(), mg.Width, mg.Height);
Console.WriteLine("Model");
Console.WriteLine(Helpers.GridToString(baseFromIds, mg.Width, mg.Height));
var model = new OverlappingModel(3);
var oh = 10;
var ow = 50;
var tm = new TileMapGenerator<char>(mg, model, 
    new ScanlineHeuristic(), 
    new SimplePropagator(3),
    ow, oh, new Random().Next());
    tm.Initialize();
    Console.WriteLine(tm.Generate());
    Console.WriteLine(Helpers.GridToString(tm.ToBase(), ow, oh));