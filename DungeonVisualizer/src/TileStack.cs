using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonVisualizer;

// A vertical slice of the map
public class TileStack : IEquatable<TileStack>
{
    public List<TileData> Tiles { get; } = [];

    public void Add(TileData data)
    {
        Tiles.Add(data);
    }

    public bool Equals(TileStack other)
    {
        if (other == null || Tiles.Count != other.Tiles.Count) return false;
        return !Tiles.Where((t, i) => !t.Equals(other.Tiles[i])).Any();
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var t in Tiles) hash.Add(t);
        return hash.ToHashCode();
    }

    public override bool Equals(object obj) => Equals(obj as TileStack);
}