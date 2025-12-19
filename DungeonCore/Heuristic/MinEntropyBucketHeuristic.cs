using System.Collections.ObjectModel;
using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public sealed class MinEntropyBucketHeuristic : IHeuristic
{
    private Random _random = new();
    
    // Entropy Data
    private double[] _stateWlw = []; 
    private double[] _cellWlwSums = [];
    
    // Bucket Data
    private List<int>[] _buckets = [];
    private int[] _indexInBucket = [];
    private int[] _currentDomainSize = [];

    private bool _initialized;

    public void Initialize(WaveGrid grid, IModel model, Random random)
    {
        _random = random;
        var cellCount = grid.CellCount;
        var maxStates = model.StateCount;
        
        // Populate Entropy Data
        double globalWlwSum = 0;
        _stateWlw = new double[maxStates];
        for (var i = 0; i < maxStates; i++)
        {
            var w = model.GetWeight(i);
            // Safety check for weight=0 to avoid NaN
            var wlw = w > 1e-9 ? w * Math.Log(w) : 0;
        
            _stateWlw[i] = wlw;
            globalWlwSum += wlw;
        }
        
        _cellWlwSums = new double[cellCount];
        for (var id = 0; id < cellCount; id++)
        {
            var cell = grid.Cells[id];
            if (cell.DomainCount == maxStates)
            {
                _cellWlwSums[id] = globalWlwSum;
                continue;
            }
            
            double currentWlwSum = 0;
            for (var s = 0; s < maxStates; s++)
            {
                if (!cell.Domain[s]) continue;
                currentWlwSum += _stateWlw[s];
            }
            _cellWlwSums[id] = currentWlwSum;
        }

        // Initialize Buckets
        _buckets = new List<int>[maxStates + 1];
        for (var i = 0; i <= maxStates; i++) _buckets[i] = [];
        _indexInBucket = new int[cellCount];
        Array.Fill(_indexInBucket, -1); // -1 means "Not in any bucket" (e.g. Observed)
        _currentDomainSize = new int[cellCount];

        // Fill Buckets
        for (var i = 0; i < cellCount; i++)
        {
            var cell = grid.Cells[i];
            
            // If the cell is already Observed, DO NOT add it to any bucket.
            if (cell.Observed != -1)
            {
                _currentDomainSize[i] = 1; // It counts as size 1
                continue; 
            }

            var count = cell.DomainCount;
            _currentDomainSize[i] = count;
            
            var bucket = _buckets[count];
            bucket.Add(i);
            _indexInBucket[i] = bucket.Count - 1; 
        }
        
        _initialized = true;
    }
    
    public void OnObserved(int cellId, int stateId)
    {
        if (!_initialized) return;
        RemoveFromBucket(cellId);
    }
    
    public void OnBanned(int cellId, int stateId)
    {
        if (!_initialized) return;

        // Update Entropy Math
        _cellWlwSums[cellId] -= _stateWlw[stateId];
        if (_cellWlwSums[cellId] < 1e-9) _cellWlwSums[cellId] = 0;

        // Update Bucket Position
        var oldSize = _currentDomainSize[cellId];
        // Ignore if removed / observed
        if (oldSize <= 1 && _indexInBucket[cellId] == -1) return;
        var newSize = oldSize - 1;
        MoveToBucket(cellId, oldSize, newSize);
    }

    private void RemoveFromBucket(int cellId)
    {
        var index = _indexInBucket[cellId];
        if (index == -1) return; // Already removed

        var currentSize = _currentDomainSize[cellId];
        var list = _buckets[currentSize];
        var lastElement = list[^1];

        // Swap-Remove
        list[index] = lastElement;
        _indexInBucket[lastElement] = index;
        list.RemoveAt(list.Count - 1);

        // Mark as removed
        _indexInBucket[cellId] = -1;
        _currentDomainSize[cellId] = 1;
    }

    private void MoveToBucket(int cellId, int oldSize, int newSize)
    {
        // Remove from the old bucket
        var oldList = _buckets[oldSize];
        var indexToRemove = _indexInBucket[cellId];
        var lastElement = oldList[^1];

        oldList[indexToRemove] = lastElement;
        _indexInBucket[lastElement] = indexToRemove;
        oldList.RemoveAt(oldList.Count - 1);

        // Move to the new bucket
        var newList = _buckets[newSize];
        newList.Add(cellId);
        _indexInBucket[cellId] = newList.Count - 1;
        _currentDomainSize[cellId] = newSize;
    }

    public int PickNextCell(WaveGrid grid)
    {
        if (!_initialized) return -1;

        // If a cell has a domain count of 1, pick it right away
        if (_buckets[1].Count > 0) return _buckets[1][0];
        
        // Find the lowest entropy in buckets 2+
        for (var k = 2; k < _buckets.Length; k++)
        {
            var candidates = _buckets[k];
            switch (candidates.Count)
            {
                case 0:
                    continue;
                case 1:
                    return candidates[0];
                default:
                    return PickFromCandidates(candidates.AsReadOnly(), grid);
            }
        }
        
        // There are no more cells to collapse
        return -1;
    }

    private int PickFromCandidates(ReadOnlyCollection<int> candidates, WaveGrid grid)
    {
        var candidate = -1;
        var minEntropy = double.PositiveInfinity;

        foreach (var cellId in candidates)
        {
            // Shannon Entropy:
            // H = log(SumWeights) - (SumWlw / SumWeights)
            var sumW = grid.Cells[cellId].SumWeights;
            var sumWlw = _cellWlwSums[cellId];
            var entropy = Math.Log(sumW) - sumWlw / sumW;

            // Add noise for organic selection
            var noise = _random.NextDouble() * 1e-4;
            var score = entropy + noise;

            if (!(score < minEntropy)) continue;
            minEntropy = score;
            candidate = cellId;
        }

        return candidate;
    }
}