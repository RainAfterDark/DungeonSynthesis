namespace DungeonCore.Shared;

public enum PropagationResult
{
    /// <summary>
    /// The wave function has successfully collapsed, and the grid is fully generated.
    /// </summary>
    Collapsed,
    /// <summary>
    /// The generation is still in progress.
    /// </summary>
    Collapsing,
    /// <summary>
    /// A contradiction was found, and the generation has failed.
    /// </summary>
    Contradicted
}
