using System.Diagnostics;
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
var model = new OverlappingModel(3);
const int oh = 28;
const int ow = 100;
var seed = Random.Shared.Next();
// seed = 1724546381;
var tm = new TileMapGenerator<char>(mg, model, 
    new MinEntropyHeuristic(), 
    new Ac4Propagator(),
    ow, oh, seed);

var sw = new Stopwatch();
sw.Start();
tm.Initialize();
var result = tm.Generate();
sw.Stop();
Console.WriteLine(tm);
Console.WriteLine($"Seed: {seed} | Domain: {model.StateCount} | {result} (took {sw.ElapsedMilliseconds}ms)");