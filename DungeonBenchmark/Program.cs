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
     │        │ 
     └──┐     │ 
        └─────┘ 
                
    """;
var (charData, width, height) = Helpers.StringToCharGrid(s);
var mg = new MappedGrid<char>(charData, width, height,'?');
var model = new OverlappingModel(3, true);
var oh = 25;
var ow = 50;
var tm = new TileMapGenerator<char>(mg, model, 
    new ScanlineHeuristic(), 
    new RecursivePropagator(),
    ow, oh, new Random().Next());
    tm.Initialize();
    Console.WriteLine(tm.Generate());
    Console.WriteLine(Helpers.GridToString(tm.ToBase(), ow, oh));