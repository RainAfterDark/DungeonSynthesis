using System.Diagnostics;
using DungeonCore;
using DungeonCore.Heuristic;
using DungeonCore.Topology;
using DungeonCore.Model;
using DungeonCore.Propagator;
using DungeonCore.Shared.Data;
using DungeonCore.Shared.Util;

const string wfc = 
    """
               
    ┌─────┐     
    │     └──┐ 
    │  WFC   │ 
    └──┐     │ 
       └─────┘ 
               
   """;
const string skyline = 
    """
    ................
    ....#...........
    ...##...@...#...
    ...##......###..
    .####......####.
    .#####..#.#####.
    ################
    ################
    """;
var (grid, width, height) = Helpers.StringToCharGrid(skyline);
var mg = new MappedGrid<char>(grid, width, height,'?');

const int oh = 28;
const int ow = 100;
var sw = new Stopwatch();
var runs = 0;
var result = PropagationResult.Contradicted;
while (result == PropagationResult.Contradicted || runs < 10)
{
    GC.Collect();
    var seed = Random.Shared.Next();
    var tm = new TileMapGenerator<char>(mg,
        new OverlappingModel(3, false),
        new OptimizedEntropyHeuristic(), 
        new Ac4Propagator(),
        ow, oh, seed);
    sw.Reset();
    sw.Start();
    result = tm.Generate(true);
    sw.Stop();
    runs++;
    // Console.WriteLine(tm);
    Console.WriteLine($"Runs: {runs} | Seed: {seed} | {result} (took {sw.ElapsedMilliseconds}ms)");
}