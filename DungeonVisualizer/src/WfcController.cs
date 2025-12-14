using System;
using System.Collections.Generic;
using System.Linq;
using DungeonCore;           
using DungeonCore.Heuristic; 
using DungeonCore.Model;     
using DungeonCore.Propagator;
using DungeonCore.Shared.Data;
using DungeonCore.Topology;  
using Godot;

namespace DungeonVisualizer;

public partial class WfcController : Node
{
    [ExportCategory("WFC Settings")]
    [Export] public PackedScene InputPatternScene;
    [Export] public Node OutputRoot;
    [Export] public Vector2I OutputSize = new(20, 20);
    [Export] public int N = 3;

    // The Palette: Maps WFC Integer IDs <-> Godot TileStacks
    private readonly Dictionary<int, TileState> _idToColumn = new();
    private readonly Dictionary<TileState, int> _columnToId = new();
    private int _nextId;

    // Captured Config to recreate layers correctly
    private TileSet _sharedTileSet;
    private readonly List<LayerConfig> _layerConfigs = [];

    private struct LayerConfig
    {
        public string Name;
        public int ZIndex;
        public bool YSortEnabled;
        public Color Modulate;
    }

    public override void _Ready()
    {
        if (InputPatternScene == null || OutputRoot == null)
        {
            GD.PrintErr("WFC: Missing InputPatternScene or OutputRoot!");
            return;
        }

        GD.Print("WFC: Starting Generation...");

        // 1. Instantiate Input
        var patternNode = InputPatternScene.Instantiate();
        AddChild(patternNode);

        // 2. Encode (Extract Grid + Capture Config)
        var (inputGrid, width, height) = EncodeInput(patternNode);
        
        // 3. Cleanup Input
        patternNode.QueueFree();

        if (inputGrid.Length == 0) return;

        DebugPalette(); // Print stats to console

        // 4. Run Library Logic
        var mapping = new MappedGrid<int>(inputGrid, width, height, -1);
        
        // Note: 'PeriodicInput' tells model if bottom connects to top
        var model = new OverlappingModel(N); 

        var generator = new TileMapGenerator<int>(
            mapping,
            model,
            new OptimizedEntropyHeuristic(), // Or MinEntropyHeuristic
            new Ac4Propagator(),
            OutputSize.X,
            OutputSize.Y,
            Random.Shared.Next()
        );
        
        // Note: If you want Periodic Output (seamless texture), pass true here if your library supports it
        // generator.SetPeriodic(PeriodicOutput); 
        
        var success = generator.Generate();

        if (success == PropagationResult.Contradicted)
            GD.PrintErr("WFC: Contradiction reached! Showing partial result.");
        else
            GD.Print("WFC: Success!");

        var outputIds = generator.ToBase();

        // 5. Decode and Paint
        ApplyToTileMap(outputIds, OutputSize.X, OutputSize.Y);
    }

    private (int[] grid, int width, int height) EncodeInput(Node patternRoot)
    {
        var layers = patternRoot.FindChildren("*", "TileMapLayer", true, false)
                                .Cast<TileMapLayer>()
                                .OrderBy(l => l.GetIndex())
                                .ToList();

        if (layers.Count == 0) 
        {
            GD.PrintErr("WFC: Input scene contains no TileMapLayers.");
            return ([], 0, 0);
        }

        // --- CAPTURE CONFIG ---
        _sharedTileSet = layers[0].TileSet;
        _layerConfigs.Clear();
        foreach (var l in layers)
        {
            _layerConfigs.Add(new LayerConfig 
            {
                Name = l.Name,
                ZIndex = l.ZIndex,
                YSortEnabled = l.YSortEnabled,
                Modulate = l.Modulate
            });
        }

        // Determine bounds
        var usedRect = layers[0].GetUsedRect();
        foreach (var l in layers) usedRect = usedRect.Merge(l.GetUsedRect());

        int w = usedRect.Size.X;
        int h = usedRect.Size.Y;
        int[] grid = new int[w * h];

        _idToColumn.Clear();
        _columnToId.Clear();
        _nextId = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2I cellCoords = usedRect.Position + new Vector2I(x, y);
                var column = new TileState();

                for (int i = 0; i < layers.Count; i++)
                {
                    var layer = layers[i];
                    var sourceId = layer.GetCellSourceId(cellCoords);

                    if (sourceId != -1)
                    {
                        // Check for Terrain Data
                        var tileData = layer.GetCellTileData(cellCoords);
                        int tSet = tileData?.TerrainSet ?? -1;
                        int tPeering = tileData?.Terrain ?? -1;

                        if (tSet != -1)
                        {
                            // It's a Terrain! Strip visual specifics.
                            column.Add(new TileData
                            {
                                LayerIndex = i,
                                SourceId = sourceId,
                                TerrainSet = tSet,
                                Terrain = tPeering,
                                // Zero out visuals so hashing groups all "Grass" together
                                AtlasCoords = Vector2I.Zero, 
                                AlternativeTile = 0 
                            });
                        }
                        else
                        {
                            // It's a Prop! Keep exact visuals.
                            column.Add(new TileData
                            {
                                LayerIndex = i,
                                SourceId = sourceId,
                                TerrainSet = -1,
                                Terrain = -1,
                                AtlasCoords = layer.GetCellAtlasCoords(cellCoords),
                                AlternativeTile = layer.GetCellAlternativeTile(cellCoords)
                            });
                        }
                    }
                }

                if (!_columnToId.TryGetValue(column, out int id))
                {
                    id = _nextId++;
                    _columnToId[column] = id;
                    _idToColumn[id] = column;
                }

                grid[y * w + x] = id;
            }
        }

        return (grid, w, h);
    }

    private void ApplyToTileMap(int[] resultGrid, int width, int height)
    {
        // 1. Clear Old Output
        foreach (var child in OutputRoot.GetChildren()) child.QueueFree();

        // 2. Create Layers
        var outLayers = new List<TileMapLayer>();
        foreach (var config in _layerConfigs)
        {
            var newLayer = new TileMapLayer();
            newLayer.TileSet = _sharedTileSet;
            newLayer.Name = config.Name;
            newLayer.ZIndex = config.ZIndex;
            newLayer.YSortEnabled = config.YSortEnabled;
            newLayer.Modulate = config.Modulate;
            OutputRoot.AddChild(newLayer);
            outLayers.Add(newLayer);
        }

        // 3. Prepare Batches for Auto-tiling
        // Key: LayerIndex -> List of (Coords, TerrainSet, TerrainPeering)
        var terrainUpdates = new Dictionary<int, List<(Vector2I, int, int)>>();

        // 4. Paint Grid
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int stateId = resultGrid[y * width + x];
                if (!_idToColumn.TryGetValue(stateId, out TileState column)) continue;

                foreach (var tile in column.Tiles)
                {
                    if (tile.LayerIndex >= outLayers.Count) continue;
                    
                    var layer = outLayers[tile.LayerIndex];
                    var coords = new Vector2I(x, y);

                    if (tile.TerrainSet >= 0)
                    {
                        // Queue Terrain
                        if (!terrainUpdates.ContainsKey(tile.LayerIndex))
                            terrainUpdates[tile.LayerIndex] = new();
                        
                        terrainUpdates[tile.LayerIndex].Add((coords, tile.TerrainSet, tile.Terrain));
                    }
                    else
                    {
                        // Paint Prop Directly
                        layer.SetCell(coords, tile.SourceId, tile.AtlasCoords, tile.AlternativeTile);
                    }
                }
            }
        }

        // 5. Execute Auto-tiling
        foreach (var (layerIdx, updates) in terrainUpdates)
        {
            var layer = outLayers[layerIdx];
            
            // Group by TerrainSet (e.g. Set 0 is Ground, Set 1 is Walls)
            var bySet = updates.GroupBy(u => u.Item2);
            
            foreach (var setGroup in bySet)
            {
                int tSet = setGroup.Key;

                // Group by Terrain Peering (e.g. Peering 0 is Grass, Peering 1 is Sand)
                // This is crucial: SetCellsTerrainConnect works best when painting ONE terrain type at a time
                var byTerrain = setGroup.GroupBy(g => g.Item3);

                foreach (var terrainGroup in byTerrain)
                {
                    int tPeering = terrainGroup.Key;
                    
                    // Convert to Godot Array for API
                    var cells = new Godot.Collections.Array<Vector2I>();
                    foreach(var item in terrainGroup) cells.Add(item.Item1);

                    // CONNECT!
                    layer.SetCellsTerrainConnect(cells, tSet, tPeering, false);
                }
            }
        }
    }

    private void DebugPalette()
    {
        GD.PrintRich($"[b]WFC Palette Report ({_idToColumn.Count} Unique States):[/b]");
        foreach (var (id, column) in _idToColumn.OrderBy(k => k.Key))
        {
            var parts = new List<string>();
            foreach (var tile in column.Tiles)
            {
                parts.Add(tile.TerrainSet != -1
                    ? $"[L{tile.LayerIndex}] Terrain (S{tile.TerrainSet}:P{tile.Terrain})"
                    : $"[L{tile.LayerIndex}] Prop ({tile.AtlasCoords})");
            }
            GD.Print($"ID {id}: {string.Join(" + ", parts)}");
        }
    }
}