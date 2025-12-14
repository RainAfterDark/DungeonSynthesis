using Godot;
using System;

namespace DungeonVisualizer;

// A single tile on a single layer
public struct TileData : IEquatable<TileData>
{
    public int LayerIndex;
    public int SourceId;
    
    // If TerrainSet >= 0, we IGNORE AtlasCoords/AlternativeTile during comparison
    public int TerrainSet; 
    public int Terrain;

    // Visuals (Only relevant if TerrainSet == -1)
    public Vector2I AtlasCoords;
    public int AlternativeTile;

    public bool Equals(TileData other)
    {
        if (LayerIndex != other.LayerIndex) return false;
        
        // LOGIC: If it's a terrain, ignore the visual specifics!
        if (TerrainSet >= 0 && other.TerrainSet >= 0)
        {
            return TerrainSet == other.TerrainSet && 
                   Terrain == other.Terrain &&
                   SourceId == other.SourceId;
        }

        // Fallback to exact visual match for non-terrain props
        return SourceId == other.SourceId && 
               AtlasCoords == other.AtlasCoords && 
               AlternativeTile == other.AlternativeTile;
    }

    public override int GetHashCode()
    {
        return TerrainSet >= 0 
            ? HashCode.Combine(LayerIndex, SourceId, TerrainSet, Terrain) 
            : HashCode.Combine(LayerIndex, SourceId, AtlasCoords, AlternativeTile);
    }

    public override bool Equals(object obj) => obj is TileData data && Equals(data);

    public static bool operator ==(TileData left, TileData right) => left.Equals(right);

    public static bool operator !=(TileData left, TileData right) => !(left == right);
}