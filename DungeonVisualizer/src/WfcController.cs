using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    [Export] public bool Periodic = true;
    [Export] public bool Symmetry;

    // The Palette: Maps WFC Integer IDs <-> Godot TileStacks
    private readonly Dictionary<int, TileStack> _idToColumn = new();
    private readonly Dictionary<TileStack, int> _columnToId = new();
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
        var patternNode = InputPatternScene.Instantiate();
        AddChild(patternNode);
        
        var (inputGrid, width, height) = EncodeInput(patternNode);
        patternNode.QueueFree();
        if (inputGrid.Length == 0) return;
        
        DebugPalette();
        var mapping = new MappedGrid<int>(inputGrid, width, height, -1);
        var path = ProjectSettings.GlobalizePath("res://../DungeonBenchmark/Resources/exported_map.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(mapping.ToMapData());
        File.WriteAllText(path, json);
        GD.Print($"Exported map to: {path}");
        
        var model = new OverlappingModel(N, Periodic, Symmetry);
        var generator = new TileMapGenerator<int>(
            mapping,
            model,
            new MinEntropyBucketHeuristic(),
            new Ac4Propagator(),
            OutputSize.X,
            OutputSize.Y,
            Random.Shared.Next()
        );
        
        var result = generator.Generate();
        GD.Print($"Model States: {model.StateCount}");
        
        if (result == PropagationResult.Contradicted)
            GD.PrintErr("WFC: Contradiction reached! Showing partial result.");
        else
            GD.Print("WFC: Success!");

        var outputIds = generator.ToBase();
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
        usedRect = layers.Aggregate(usedRect, (current, l) => current.Merge(l.GetUsedRect()));

        var w = usedRect.Size.X;
        var h = usedRect.Size.Y;
        var grid = new int[w * h];

        _idToColumn.Clear();
        _columnToId.Clear();
        _nextId = 0;

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var cellCoords = usedRect.Position + new Vector2I(x, y);
                var column = new TileStack();

                for (var i = 0; i < layers.Count; i++)
                {
                    var layer = layers[i];
                    var sourceId = layer.GetCellSourceId(cellCoords);

                    if (sourceId == -1) continue;
                    var tileData = layer.GetCellTileData(cellCoords);
                    var tSet = tileData?.TerrainSet ?? -1;
                    var tId = tileData?.Terrain ?? -1;

                    if (tSet != -1) // If it's in a terrain set
                    {
                        column.Add(new TileData
                        {
                            LayerIndex = i,
                            SourceId = sourceId,
                            TerrainSet = tSet,
                            Terrain = tId,
                            // Zero out visuals to hash groups of the same terrain together
                            AtlasCoords = Vector2I.Zero, 
                            AlternativeTile = 0 
                        });
                    }
                    else // Keep exact details for the rest
                    {
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

                if (!_columnToId.TryGetValue(column, out var id))
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
        // Clear old output
        foreach (var child in OutputRoot.GetChildren()) child.QueueFree();

        // Create layers
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

        // Prepare batches for auto-tiling
        // Key: LayerIndex -> List of (Coords, TerrainSet, TerrainID)
        var terrainUpdates = new Dictionary<int, List<(Vector2I, int, int)>>();

        // Paint grid
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var stateId = resultGrid[y * width + x];
                if (!_idToColumn.TryGetValue(stateId, out var column)) continue;

                foreach (var tile in column.Tiles)
                {
                    if (tile.LayerIndex >= outLayers.Count) continue;
                    
                    var layer = outLayers[tile.LayerIndex];
                    var coords = new Vector2I(x, y);

                    if (tile.TerrainSet >= 0)
                    {
                        // Queue terrains
                        if (!terrainUpdates.ContainsKey(tile.LayerIndex))
                            terrainUpdates[tile.LayerIndex] = [];
                        terrainUpdates[tile.LayerIndex].Add((coords, tile.TerrainSet, tile.Terrain));
                    }
                    else
                    {
                        // Paint the rest directly
                        layer.SetCell(coords, tile.SourceId, tile.AtlasCoords, tile.AlternativeTile);
                    }
                }
            }
        }

        // Execute Auto-tiling
        foreach (var (layerIdx, updates) in terrainUpdates)
        {
            var layer = outLayers[layerIdx];
            
            // Group by TerrainSet (e.g., Set 0 is Ground, Set 1 is Walls)
            var bySet = updates.GroupBy(u => u.Item2);
            
            foreach (var setGroup in bySet)
            {
                var tSet = setGroup.Key;

                // Group by Terrain ID (e.g., ID 0 is Grass, ID 1 is Sand)
                // This is crucial: SetCellsTerrainConnect works best when painting ONE terrain type at a time
                var byTerrain = setGroup.GroupBy(g => g.Item3);

                foreach (var terrainGroup in byTerrain)
                {
                    var tId = terrainGroup.Key;
                    
                    // Convert to Godot Array for API
                    var cells = new Godot.Collections.Array<Vector2I>();
                    foreach(var item in terrainGroup) cells.Add(item.Item1);

                    // CONNECT!
                    layer.SetCellsTerrainConnect(cells, tSet, tId, false);
                }
            }
        }
    }

    private void DebugPalette()
    {
        GD.Print($"WFC Palette Report ({_idToColumn.Count} Unique Tiles):");
        foreach (var (id, column) in _idToColumn.OrderBy(k => k.Key))
        {
            var parts = column.Tiles.Select(tile => tile.TerrainSet != -1
                    ? $"[L{tile.LayerIndex}] Terrain (S{tile.TerrainSet}:P{tile.Terrain})"
                    : $"[L{tile.LayerIndex}] Sprite ({tile.AtlasCoords})")
                .ToList();
            GD.Print($"ID {id}: {string.Join(" + ", parts)}");
        }
    }
}