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
var oh = 25;
var ow = 50;
var seed = Random.Shared.Next();
// Contradictions:
// seed = 1698121561; // scanline
// seed = 1365822617; // min-entropy
Console.WriteLine($"Seed: {seed}");
var tm = new TileMapGenerator<char>(mg, model, 
    new MinEntropyHeuristic(), 
    new Ac3Propagator(),
    ow, oh, seed);
    tm.Initialize();
    Console.WriteLine(tm.Generate());
    Console.WriteLine(Helpers.GridToString(tm.ToBase(), ow, oh));