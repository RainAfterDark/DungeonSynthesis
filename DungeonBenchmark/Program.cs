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
     │  WFC   │ 
     └──┐     │ 
        └─────┘ 
                
    """;
var (charData, width, height) = Helpers.StringToCharGrid(s);
var mg = new MappedGrid<char>(charData, width, height,'?');
var model = new OverlappingModel(3, true);
var oh = 29;
var ow = 69;
var seed = Random.Shared.Next();
Console.Write($"Seed: {seed} -> ");
var tm = new TileMapGenerator<char>(mg, model, 
    new MinEntropyHeuristic(), 
    new Ac4Propagator(),
    ow, oh, seed);
    tm.Initialize();
    Console.WriteLine(tm.Generate());
    Console.WriteLine(Helpers.GridToString(tm.ToBase(), ow, oh));