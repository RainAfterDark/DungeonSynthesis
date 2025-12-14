using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public sealed class MinEntropyHeuristic : IHeuristic
{
    private Random _random = new();
    private double[] _stateWlw = []; // Precomputed (Weight * Log(Weight)) for each state ID
    private double[] _cellWlwSums = []; // The shadow array tracking Sum(Wlw) per cell
    private bool _initialized;

    public void Initialize(WaveGrid grid, IModel model, Random random)
    {
        _random = random;
        var cellCount = grid.CellCount;
        var maxStates = model.StateCount;
        
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
        
        _initialized = true;
    }
    
    public void OnBanned(int cellId, int stateId)
    {
        if (!_initialized) return;
        _cellWlwSums[cellId] -= _stateWlw[stateId];
        if (_cellWlwSums[cellId] < 1e-9) _cellWlwSums[cellId] = 0;
    }

    public int PickNextCell(WaveGrid grid)
    {
        if (!_initialized) return -1;
        var candidate = -1;
        var minEntropy = double.PositiveInfinity;

        for (var i = 0; i < grid.CellCount; i++)
        {
            var cell = grid.Cells[i];

            // Skip if collapsed
            if (cell.Observed != -1) continue;

            // Skip if contradiction (safety check)
            if (cell.DomainCount < 1) continue;

            // Shannon Entropy:
            // H = log(SumWeights) - (SumWlw / SumWeights)
            var sumW = cell.SumWeights;
            var sumWlw = _cellWlwSums[i];
            var entropy = Math.Log(sumW) - sumWlw / sumW;

            // Add noise for organic selection
            var noise = _random.NextDouble() * 1e-4;
            var score = entropy + noise;

            if (!(score < minEntropy)) continue;
            minEntropy = score;
            candidate = i;
        }

        return candidate;
    }
}