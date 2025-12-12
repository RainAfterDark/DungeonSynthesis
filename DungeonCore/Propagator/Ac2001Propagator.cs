using System.Runtime.CompilerServices;
using DungeonCore.Model;
using DungeonCore.Shared;
using DungeonCore.Topology;

namespace DungeonCore.Propagator;

public sealed class Ac2001Propagator : IPropagator
{
    // Layout: Cell -> State -> Direction
    // // [Cell * (StateCount * 4) + State * 4 + Direction]
    // Stores the index 'k' for the Model's neighbor list where we last found support.
    private int[] _lastSupport = [];
    private int _stateCount;
    private int _stateDirCount;
    private bool _initialized;
    private readonly Stack<int> _dirtyStack = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetPointerIndex(int cellId, int stateId, int dir) => cellId * _stateDirCount + (stateId << 2) + dir;

    public void Initialize(WaveGrid grid, IModel model)
    {
        const int dirCount = 4;
        _stateCount = model.StateCount;
        _stateDirCount = _stateCount * dirCount;
        _lastSupport = new int[grid.CellCount * _stateDirCount];
        _initialized = true;
    }

    public bool Collapse(WaveGrid grid, IModel model, int cellId)
    {
        if (!_initialized) Initialize(grid, model);
        
        var cell = grid.Cells[cellId];
        var chosenState = model.PickState(cell);
        if (chosenState == -1 || !grid.Observe(cellId, chosenState)) return false;
        
        _dirtyStack.Clear();
        _dirtyStack.Push(cellId);
        return Propagate(grid, model);
    }

    private bool Propagate(WaveGrid grid, IModel model)
    {
        var lastSupport = _lastSupport;
        while (_dirtyStack.Count > 0)
        {
            var currentCellId = _dirtyStack.Pop();
            var currentCell = grid.Cells[currentCellId];

            // Notify all neighbors that 'currentCell' has changed.
            foreach (var (neighborId, dir) in grid.NeighborsOf(currentCellId))
            {
                var nCell = grid.Cells[neighborId];
                if (nCell.Observed != -1) continue; 

                var neighborChanged = false;
                var oppositeDir = Direction.Invert(dir);

                // Check every state that is CURRENTLY valid in the neighbor.
                for (var nState = 0; nState < model.StateCount; nState++)
                {
                    if (!nCell.Domain[nState]) continue;
                    // 1. Get the list of states that 'nState' ALLOWS in 'currentCell'
                    // (i.e. "If I am nState, what can be inside currentCell?")
                    var candidates = model.GetNeighbors(nState, oppositeDir);

                    // 2. Get the pointer to where we last found support
                    // Note: We index based on the NEIGHBOR because we are tracking the support FOR the neighbor state.
                    // Pointer Index = NeighborCell -> NeighborState -> DirectionToCurrent
                    var pointerIdx = GetPointerIndex(neighborId, nState, oppositeDir);
                    var resumeIndex = lastSupport[pointerIdx];

                    // 3. AC-2001 Search
                    // Resume searching from 'resumeIndex'. We do NOT restart at 0.
                    var foundSupport = false;
                    var count = candidates.Count;
                    
                    for (var k = resumeIndex; k < count; k++)
                    {
                        var candidateState = candidates[k];
                        // Is this candidate still active in the recently modified currentCell?
                        if (!currentCell.Domain[candidateState]) continue;
                        // Success! Update the pointer so next time we start here.
                        lastSupport[pointerIdx] = k;
                        foundSupport = true;
                        break;
                    }

                    // 4. If we ran off the end of the list, no support exists. Ban it.
                    if (foundSupport) continue;
                    // Optimization: set pointer to end to avoid re-scanning fully on next pass
                    lastSupport[pointerIdx] = count; 
                        
                    var weight = model.GetWeight(nState);
                    if (!grid.Ban(neighborId, nState, weight)) continue;
                    if (nCell.DomainCount == 0) return false;
                    neighborChanged = true;
                }

                if (!neighborChanged) continue;
                _dirtyStack.Push(neighborId);
            }
        }

        return true;
    }
}