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

const int oh = 28;
const int ow = 100;
var sw = new Stopwatch();
TileMapGenerator<char>? tm = null;
int seed = 0;
int runs = 0;
PropagationResult result = PropagationResult.Contradicted;
while (result == PropagationResult.Contradicted || runs < 10)
{
    GC.Collect();
    seed = Random.Shared.Next();
    // seed = 1190156738;
    tm = new TileMapGenerator<char>(mg,
        new OverlappingModel(3),
        new OptimizedEntropyHeuristic(), 
        new Ac4Propagator(),
        ow, oh, seed);
    sw.Reset();
    sw.Start();
    tm.Initialize();
    result = tm.Generate();
    sw.Stop();
    runs++;
}
Console.WriteLine(tm);
Console.WriteLine($"Runs: {runs} | Seed: {seed} | {result} (took {sw.ElapsedMilliseconds}ms)");