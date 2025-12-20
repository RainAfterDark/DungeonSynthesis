using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DungeonCore;
using DungeonCore.Heuristic;
using DungeonCore.Model;
using DungeonCore.Propagator;
using DungeonCore.Topology;

namespace DungeonBenchmark;

[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[MemoryDiagnoser]
[HtmlExporter]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class AlgorithmBenchmark
{
    public enum HeuristicType { Scanline, MinEntropy, MinEntropyHeap, MinEntropyBucket }
    public enum PropagatorType { Recursive, Ac3, Ac2001, Ac4 }

    [ParamsAllValues]
    public HeuristicType Heuristic;
    
    [ParamsAllValues]
    public PropagatorType Propagator;
    
    private MappedGrid<int> _inputMapping = null!;

    [GlobalSetup]
    public void Setup()
    {
        var json = File.ReadAllText("Resources/exported_map.json");
        var mapData = JsonSerializer.Deserialize<MapData<int>>(json) ?? throw new InvalidOperationException();
        _inputMapping = MappedGrid<int>.FromMapData(mapData);
    }

    [Benchmark]
    public void Wfc()
    {
        IHeuristic heuristic = Heuristic switch
        {
            HeuristicType.Scanline => new ScanlineHeuristic(),
            HeuristicType.MinEntropy => new MinEntropyHeuristic(),
            HeuristicType.MinEntropyHeap => new MinEntropyHeapHeuristic(),
            HeuristicType.MinEntropyBucket => new MinEntropyBucketHeuristic(),
            _ => throw new ArgumentOutOfRangeException()
        };
        IPropagator propagator = Propagator switch
        {
            PropagatorType.Recursive => new RecursivePropagator(),
            PropagatorType.Ac3 => new Ac3Propagator(),
            PropagatorType.Ac2001 => new Ac2001Propagator(),
            PropagatorType.Ac4 => new Ac4Propagator(),
            _ => throw new ArgumentOutOfRangeException()
        };

        var tilemap = new TileMapGenerator<int>(_inputMapping,
            new OverlappingModel(3), 
            heuristic, propagator, 40, 24);
        tilemap.GenerateUntilCollapsed();
    }
}