using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DungeonCore;
using DungeonCore.Heuristic;
using DungeonCore.Model;
using DungeonCore.Propagator;
using DungeonCore.Shared.Util;
using DungeonCore.Topology;

namespace DungeonBenchmark;

[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 20)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[MemoryDiagnoser]
[HtmlExporter]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class DataStructureBenchmark
{
    private const string InputPattern =
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

    [Params(30, 100)]
    public int OutputSize;

    [Params(2, 3)]
    public int N;
    
    private MappedGrid<char> _inputMapping = null!;

    [GlobalSetup]
    public void Setup()
    {
        var (grid, width, height) = Helpers.StringToCharGrid(InputPattern);
        _inputMapping = new MappedGrid<char>(grid, width, height, '?');
    }

    [Benchmark]
    public void BucketHeuristic()
    {
        var generator = new TileMapGenerator<char>(
            _inputMapping,
            new OverlappingModel(N),
            new MinEntropyBucketHeuristic(),
            new Ac4Propagator(),
            OutputSize,
            OutputSize
        );

        generator.GenerateUntilCollapsed();
    }

    [Benchmark]
    public void HeapHeuristic()
    {
        var generator = new TileMapGenerator<char>(
            _inputMapping,
            new OverlappingModel(N),
            new MinEntropyHeapHeuristic(),
            new Ac4Propagator(),
            OutputSize,
            OutputSize
        );

        generator.GenerateUntilCollapsed();
    }

    [Benchmark(Baseline = true)]
    public void NaiveHeuristic()
    {
        var generator = new TileMapGenerator<char>(
            _inputMapping,
            new OverlappingModel(N),
            new MinEntropyHeuristic(),
            new Ac4Propagator(),
            OutputSize,
            OutputSize
        );

        generator.GenerateUntilCollapsed();
    }
}