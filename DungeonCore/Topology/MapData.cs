namespace DungeonCore.Topology;

public record MapData<TBase>(TBase[] Grid, int Width, int Height, TBase UnknownValue);