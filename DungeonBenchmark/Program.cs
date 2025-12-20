using BenchmarkDotNet.Running;
using DungeonBenchmark;
using DungeonCore;
using DungeonCore.Heuristic;
using DungeonCore.Model;
using DungeonCore.Propagator;
using DungeonCore.Shared.Util;
using DungeonCore.Topology;

#if DEBUG
    const string input = 
        """
        
         ┌────────────────┬────────┐ 
         │                │        │ 
         │   ┌───────┐    │    ┌───┤ 
         │   │       │    │    │   │ 
         ├───┘       └────┼────┘   │ 
         │                │        │ 
         │   ┌────┐   ┌───┴────┐   │ 
         │   │    ├───┤        │   │ 
         │   └─┬──┘   └───┐    │   │ 
         │     │          │    │   │ 
         └─────┴──────────┴────┴───┘ 
        
        """;

    var (grid, width, height) = Helpers.StringToCharGrid(input);
    var mapping = new MappedGrid<char>(grid, width, height, '?');
    var model = new OverlappingModel(3);
    var tilemap = new TileMapGenerator<char>(mapping, model,
        new MinEntropyBucketHeuristic(),
        new Ac4Propagator(),
        100, 30);
    tilemap.GenerateUntilCollapsed(true);
    Console.WriteLine($"States: {model.StateCount}");
    Console.WriteLine("Please switch to Release mode to run benchmarks!");
#else
    var summary = BenchmarkRunner.Run<AlgorithmBenchmark>();
#endif