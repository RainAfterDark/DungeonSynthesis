using System.Collections;

namespace DungeonCore.Topology;

public class WaveCell(int states)
{
    public int Observed { get; private set; } = -1;
    public BitArray Domain { get; } = new(states, true);
    public int DomainCount { get; private set; } = states;

    public void SetObserved(int state)
    {
        Observed = state;
        for (var i = 0; i < Domain.Length; i++) 
            Domain[i] = i == state;
        DomainCount = 1;
    }

    public bool Ban(int state)
    {
        if (!Domain[state]) return false;
        Domain[state] = false;
        DomainCount--;
        return true;
    }
}